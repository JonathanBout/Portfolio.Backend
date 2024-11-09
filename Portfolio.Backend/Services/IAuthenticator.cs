global using RefreshTokenData = (string token, uint id, System.DateTimeOffset expiration);
using Portfolio.Backend.Data.Users;


namespace Portfolio.Backend.Services
{
	public interface IAuthenticator
	{
		(string accessToken, RefreshTokenData newRefreshToken)? GetAccessToken(string email, uint refreshTokenId, string refreshToken);
		RefreshTokenData? GetRefreshToken(string email, string password);
		bool RevokeRefreshToken(User owner, uint tokenId);
		void RevokeAllRefreshTokens(User owner);
		IEnumerable<RefreshToken> GetRefreshTokens(User owner);
		TimeSpan? BeginPasswordReset(User user);
		bool CompletePasswordReset(User user, string token, string newPassword);
	}
}
