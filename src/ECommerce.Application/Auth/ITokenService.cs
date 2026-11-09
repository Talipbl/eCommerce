using ECommerce.Accounts;

namespace ECommerce.Application.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(IAccount account, DateTime now);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
