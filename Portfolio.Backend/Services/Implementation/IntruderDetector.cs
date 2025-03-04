using Portfolio.Backend.Data.Users;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Portfolio.Backend.Services.Implementation
{
	public class IntruderDetector(ICryptoHelper cryptoHelper, IServiceProvider services) : IIntruderDetector, IHostedService
	{
		private readonly ICryptoHelper _crypto = cryptoHelper;
		private readonly IServiceProvider _serviceProvider = services;
		private readonly CancellationTokenSource _backgroundServiceCts = new();
		private Task? _backgroundServiceTask = null;

		private readonly ConcurrentQueue<Func<IServiceProvider, CancellationToken, Task>> _work = new();

		public void EnqueueInvalidAccessTokenUsage(uint tokenId, string usedToken)
		{
			_work.Enqueue((sp, ct) =>
			{
				var db = sp.GetRequiredService<DatabaseContext>();
				if (db.Set<RefreshToken>().Find(tokenId) is RefreshToken token)
				{
					// check if the token is an old one. At most 10, as the verification is quite an expensive operation
					foreach (var oldToken in token.Values.OrderBy(v => v.CreationDate))
					{
						if (ct.IsCancellationRequested)
							return Task.CompletedTask;

						if (_crypto.Verify(usedToken, oldToken.TokenHash) == VerificationResult.Success)
						{
							// the token is an old one, this means someone is trying to reuse an old token.
							// this is known as a replay attack and we should not allow it.
							// For the sake of security, we will completely invalidate the token

							var authenticationManager = sp.GetRequiredService<IAuthenticator>();

							authenticationManager.RevokeRefreshToken(token.Owner, token.Id);
							break;
						}
					}
				}

				return Task.CompletedTask;
			});

			Awake();
		}

		private void Awake()
		{
			if (_backgroundServiceTask is not null)
				return;

			_backgroundServiceTask = Task.Run(async () =>
			{
				while (!_backgroundServiceCts.Token.IsCancellationRequested)
				{
					if (_work.TryDequeue(out var work))
					{
						using var scope = _serviceProvider.CreateScope();
						using var cts = CancellationTokenSource.CreateLinkedTokenSource(_backgroundServiceCts.Token);

						var workTask = Task.Run(async () => await work(scope.ServiceProvider, cts.Token), cts.Token);

						// we want to make sure that we don't block the queue for too long.
						// we will set a timeout based on the amount of work in the queue
						var timeout = int.Max(10_000 / int.Max(1, _work.Count), 1000);
#if DEBUG
						if (Debugger.IsAttached)
						{
							cts.CancelAfter(timeout);
						}
#endif
						await workTask;
					} else
					{
						return;
					}
				}
			}).ContinueWith(_ =>
			{
				_backgroundServiceTask?.Dispose();
				_backgroundServiceTask = null;
			});
		}

		public Task StartAsync(CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
		public Task StopAsync(CancellationToken cancellationToken) => _backgroundServiceCts.CancelAsync();
	}
}
