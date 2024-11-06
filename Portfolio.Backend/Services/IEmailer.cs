using Portfolio.Backend.Data.Users;
using System.Net.Mail;

namespace Portfolio.Backend.Services
{
	public interface IEmailer
	{
		Task SendEmailAsync<TEmailProvider>(User user, Action<TEmailProvider> configure) where TEmailProvider : IEmailProvider;
	}

	public interface IEmailProvider
	{
		Task<MailMessage> CreateEmail(User user);
	}

	public interface IResetPasswordEmailProvider : IEmailProvider
	{
		public string ResetToken { get; set; }
	}
}