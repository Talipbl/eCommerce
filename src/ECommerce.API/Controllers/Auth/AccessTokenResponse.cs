namespace ECommerce.API.Controllers.Auth;

public record AccessTokenResponse(string AccessToken, DateTime ExpiresAt, string TokenType = "Bearer");
