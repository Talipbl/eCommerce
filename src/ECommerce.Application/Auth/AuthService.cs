using ECommerce.Admin;
using ECommerce.Core.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace ECommerce.Application.Auth;

public class AuthService(
    IRepository<DefAdmin> admins,
    IRepository<AdminRefreshToken> refreshTokens,
    IUnitOfWork unitOfWork,
    IPasswordHasher<DefAdmin> passwordHasher,
    ITokenService tokenService,
    IOptions<AuthOptions> options,
    TimeProvider clock)
    : AccountAuthService<DefAdmin, AdminRefreshToken>(admins, refreshTokens, unitOfWork, passwordHasher, tokenService, options, clock), IAuthService
{
    public async Task<AdminProfile?> GetProfileAsync(int adminId, CancellationToken cancellationToken = default)
    {
        var admin = await Accounts.GetByIdAsync(adminId, cancellationToken);
        return admin is null
            ? null
            : new AdminProfile(admin.Id, admin.UserName, admin.FirstName, admin.LastName, admin.MailAdress, admin.Role, admin.LastLoginDate);
    }

    protected override Expression<Func<DefAdmin, bool>> LoginEquals(string login) => a => a.UserName == login;

    protected override Expression<Func<AdminRefreshToken, bool>> TokenHashEquals(string tokenHash) => t => t.TokenHash == tokenHash;

    protected override Expression<Func<AdminRefreshToken, bool>> ActiveTokensOf(int accountId) => t => t.AdminId == accountId && t.RevokedAt == null;
}
