using Microsoft.Extensions.Options;
using Portfolio.Backend.Configuration;
using Portfolio.Backend.Data.Users;
using System.Net;
using System.Net.Mail;

namespace Portfolio.Backend.Services.Implementation
{
	public class EmailSender(IServiceProvider services, IOptionsMonitor<EmailConfiguration> emailOptions, ILogger<EmailSender> logger) : IEmailer
	{
		private readonly IServiceProvider _services = services;
		private readonly IOptionsMonitor<EmailConfiguration> _emailConfiguration = emailOptions;
		private readonly ILogger<EmailSender> _logger = logger;

		public async Task SendEmailAsync<TEmailProvider>(User user, Action<TEmailProvider> configure) where TEmailProvider : IEmailProvider
		{
			int retries = 0;
			SmtpClient? client = null;
			do
			{
				try
				{
					var sender = _services.GetRequiredService<TEmailProvider>();

					configure(sender);

					using var email = await sender.CreateEmail(user);

					var from = new MailAddress(_emailConfiguration.CurrentValue.FromEmail, _emailConfiguration.CurrentValue.FromName);
					email.Sender = from;
					email.From = from;

					client ??= CreateClient();


					await client.SendMailAsync(email);

					break;
				} catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to send email provided by {Provider} to user {User} (attempt {Attempt})", typeof(TEmailProvider).Name, user.Email, retries + 1);
				}
			} while (++retries < 3);

			client?.Dispose();
		}

		private SmtpClient CreateClient()
		{
			if (string.IsNullOrWhiteSpace(_emailConfiguration.CurrentValue.SmtpServer))
			{
				var emailPath = Path.Combine(Directory.GetCurrentDirectory(), "emails");

				if (!Directory.Exists(emailPath))
					Directory.CreateDirectory(emailPath);

				return new SmtpClient
				{
					DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
					PickupDirectoryLocation = emailPath,
				};
			} else
			{
				return new SmtpClient(_emailConfiguration.CurrentValue.SmtpServer, _emailConfiguration.CurrentValue.Port)
				{
					Credentials = new NetworkCredential(_emailConfiguration.CurrentValue.Username, _emailConfiguration.CurrentValue.GetPassword()),
					EnableSsl = _emailConfiguration.CurrentValue.UseSSL,
				};
			}
		}
	}
}
