using Portfolio.Backend.Data.Users;

namespace Portfolio.Backend.Services.Implementation
{
	public class UserService(DatabaseContext database) : IUserService
	{
		private readonly DatabaseContext _database = database;
		public User? GetUser(uint id) => _database.Users.Find(id);
		public User? GetUserByEmail(string email) => _database.Users.FirstOrDefault(u => u.Email == email);
		public User? GetUserBySlug(string slug) => _database.Users.FirstOrDefault(u => u.NameSlug == slug.ToLower());
		public bool UpdateUser(User user)
		{
			_database.Users.Update(user);
			return _database.SaveChanges() > 0;
		} 
	}
}
