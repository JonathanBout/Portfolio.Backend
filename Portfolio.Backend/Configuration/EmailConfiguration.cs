using System.ComponentModel.DataAnnotations;

namespace Portfolio.Backend.Configuration
{
	public class EmailConfiguration
	{
		public string SmtpServer { get; set; } = string.Empty;

		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;

		[Range(1, ushort.MaxValue)]
		public int Port { get; set; } = 25;

		public bool UseSSL { get; set; } = true;

		public string FromEmail { get; set; } = string.Empty;

		public string FromName { get; set; } = string.Empty;
	}
}
