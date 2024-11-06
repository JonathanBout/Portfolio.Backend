using Portfolio.Backend.Data.Users;

namespace Portfolio.Backend.Services
{
	public interface IAuthenticator
	{
		string? GetAccessToken(string email, uint refreshTokenId, string refreshToken);
		(string token, uint id)? GetRefreshToken(string email, string password);
		bool RevokeRefreshToken(User owner, uint tokenId);
		void RevokeAllRefreshTokens(User owner);
		IEnumerable<RefreshToken> GetRefreshTokens(User owner);
		TimeSpan? BeginPasswordReset(User user);
		bool CompletePasswordReset(User user, string token, string newPassword);
	}
}
