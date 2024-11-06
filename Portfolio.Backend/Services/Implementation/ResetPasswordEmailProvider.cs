using Portfolio.Backend.Data.Users;
using System.Net.Mail;

namespace Portfolio.Backend.Services.Implementation
{
	public class ResetPasswordEmailProvider : IResetPasswordEmailProvider
	{
		public string ResetToken { get; set; } = "";

		public Task<MailMessage> CreateEmail(User user)
		{
			var message = new MailMessage
			{
				Subject = "Reset your password",
				Body = $"""
					Hi {user.FullName},

					You requested to reset your password. Use code <b>{ResetToken}</b> to reset your password.

					If you didn't request this, you can safely ignore this email.

					Thanks!
					""",
				IsBodyHtml = true,
			};
			message.Bcc.Add(user.Email);
			return Task.FromResult(message);
		}
	}
}