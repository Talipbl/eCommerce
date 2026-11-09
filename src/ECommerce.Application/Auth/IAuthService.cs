namespace ECommerce.Application.Auth;

public interface IAuthService
{
    Task<AuthResult<AuthTokens>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<AuthResult<bool>> ChangePasswordAsync(int adminId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<AdminProfile?> GetProfileAsync(int adminId, CancellationToken cancellationToken = default);
}
