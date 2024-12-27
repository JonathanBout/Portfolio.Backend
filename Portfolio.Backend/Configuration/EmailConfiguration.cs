using System.ComponentModel.DataAnnotations;

namespace Portfolio.Backend.Configuration
{
	public class EmailConfiguration
	{
		public string SmtpServer { get; set; } = string.Empty;

		public string Username { get; set; } = string.Empty;
		public string? Password { get; set; }
		public string? PasswordFilePath { get; set; }

		public string GetPassword()
		{
			if (Password is not null) return Password;
			if (PasswordFilePath is not null) return PasswordFilePath;

			throw new InvalidOperationException("Either Password or PasswordFilePath should be configured.");
		}

		[Range(1, ushort.MaxValue)]
		public int Port { get; set; } = 25;

		public bool UseSSL { get; set; } = true;

		public string FromEmail { get; set; } = string.Empty;

		public string FromName { get; set; } = string.Empty;
	}
}
