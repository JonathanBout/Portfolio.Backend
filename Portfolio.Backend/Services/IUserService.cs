using Portfolio.Backend.Data.Users;

namespace Portfolio.Backend.Services
{
	public interface IUserService
	{
		public User? GetUser(uint id);
		public User? GetUserByEmail(string email);
		public User? GetUserBySlug(string slug);
		public bool UpdateUser(User user);
	}
}
