namespace ECommerce.Application.Auth;

public record LoginRequest(string UserName, string Password);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record AuthTokens(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);

public record AdminProfile(int Id, string UserName, string FirstName, string LastName, string Email, string Role, DateTime? LastLoginDate);

public enum AuthError
{
    None,
    InvalidCredentials,
    LockedOut,
    InvalidRefreshToken,
    NotFound
}

public record AuthResult<T>(bool Succeeded, T? Value, AuthError Error)
{
    public static AuthResult<T> Success(T value) => new(true, value, AuthError.None);

    public static AuthResult<T> Failure(AuthError error) => new(false, default, error);
}
