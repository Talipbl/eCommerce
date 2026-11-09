using ECommerce.Application.Auth;
using ECommerce.Application.Common;

namespace ECommerce.Application.Customers;

public interface ICustomerAuthService
{
    Task<Result<AuthTokens>> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult<AuthTokens>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<AuthResult<bool>> ChangePasswordAsync(int customerId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<CustomerProfileDto?> GetProfileAsync(int customerId, CancellationToken cancellationToken = default);
    Task<Result<CustomerProfileDto>> UpdateProfileAsync(int customerId, UpdateCustomerProfileRequest request, CancellationToken cancellationToken = default);
}
