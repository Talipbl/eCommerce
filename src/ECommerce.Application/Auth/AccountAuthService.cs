using ECommerce.Accounts;
using ECommerce.Core.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace ECommerce.Application.Auth;

/// <summary>
/// Yönetici ve müşteri girişinin ortak akışı: parola doğrulama, kilitleme, refresh token rotasyonu ve tekrar kullanım tespiti.
/// </summary>
public abstract class AccountAuthService<TAccount, TToken>(
    IRepository<TAccount> accounts,
    IRepository<TToken> refreshTokens,
    IUnitOfWork unitOfWork,
    IPasswordHasher<TAccount> passwordHasher,
    ITokenService tokenService,
    IOptions<AuthOptions> options,
    TimeProvider clock)
    where TAccount : class, IAccount, ECommerce.Core.Domain.IEntity
    where TToken : class, IRefreshToken<TToken>, ECommerce.Core.Domain.IEntity
{
    private readonly AuthOptions _options = options.Value;

    protected DateTime Now => clock.GetUtcNow().UtcDateTime;

    protected IRepository<TAccount> Accounts => accounts;

    protected IUnitOfWork UnitOfWork => unitOfWork;

    protected abstract Expression<Func<TAccount, bool>> LoginEquals(string login);

    protected abstract Expression<Func<TToken, bool>> TokenHashEquals(string tokenHash);

    protected abstract Expression<Func<TToken, bool>> ActiveTokensOf(int accountId);

    public async Task<AuthResult<AuthTokens>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var account = await accounts.FirstOrDefaultAsync(LoginEquals(request.UserName), cancellationToken);
        if (account is null || !account.IsActive)
            return AuthResult<AuthTokens>.Failure(AuthError.InvalidCredentials);

        var now = Now;
        if (account.IsLockedOut(now))
            return AuthResult<AuthTokens>.Failure(AuthError.LockedOut);

        var verification = VerifyPassword(account, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            account.RegisterFailedLogin(now, _options.MaxFailedLoginAttempts, TimeSpan.FromMinutes(_options.LockoutMinutes));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AuthResult<AuthTokens>.Failure(account.IsLockedOut(now) ? AuthError.LockedOut : AuthError.InvalidCredentials);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            account.PasswordHash = passwordHasher.HashPassword(account, request.Password);

        account.RegisterSuccessfulLogin(now);
        var tokens = IssueTokens(account, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResult<AuthTokens>.Success(tokens);
    }

    public async Task<AuthResult<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var stored = await refreshTokens.FirstOrDefaultAsync(TokenHashEquals(tokenService.HashRefreshToken(refreshToken)), cancellationToken);
        if (stored is null)
            return AuthResult<AuthTokens>.Failure(AuthError.InvalidRefreshToken);

        var now = Now;
        if (!stored.IsActive(now))
        {
            // Döndürülmüş bir token'ın tekrar kullanılması çalınma belirtisidir; tüm oturumlar kapatılır.
            if (stored.RevokedAt is not null)
            {
                await RevokeAllAsync(stored.AccountId, now, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return AuthResult<AuthTokens>.Failure(AuthError.InvalidRefreshToken);
        }

        var account = await accounts.GetByIdAsync(stored.AccountId, cancellationToken);
        if (account is null || !account.IsActive || account.IsLockedOut(now))
        {
            stored.Revoke(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AuthResult<AuthTokens>.Failure(AuthError.InvalidRefreshToken);
        }

        var tokens = IssueTokens(account, now);
        stored.Revoke(now, tokenService.HashRefreshToken(tokens.RefreshToken));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResult<AuthTokens>.Success(tokens);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var stored = await refreshTokens.FirstOrDefaultAsync(TokenHashEquals(tokenService.HashRefreshToken(refreshToken)), cancellationToken);
        if (stored is null || stored.RevokedAt is not null)
            return;

        stored.Revoke(Now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthResult<bool>> ChangePasswordAsync(int accountId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var account = await accounts.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
            return AuthResult<bool>.Failure(AuthError.NotFound);

        if (VerifyPassword(account, request.CurrentPassword) == PasswordVerificationResult.Failed)
            return AuthResult<bool>.Failure(AuthError.InvalidCredentials);

        account.PasswordHash = passwordHasher.HashPassword(account, request.NewPassword);
        await RevokeAllAsync(account.Id, Now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResult<bool>.Success(true);
    }

    protected string HashPassword(TAccount account, string password) => passwordHasher.HashPassword(account, password);

    protected AuthTokens IssueTokens(TAccount account, DateTime now)
    {
        var (accessToken, accessExpiresAt) = tokenService.CreateAccessToken(account, now);
        var refreshToken = tokenService.CreateRefreshToken();
        var refreshExpiresAt = now.AddDays(_options.RefreshTokenDays);

        refreshTokens.Add(TToken.Create(account.Id, tokenService.HashRefreshToken(refreshToken), refreshExpiresAt));

        return new AuthTokens(accessToken, accessExpiresAt, refreshToken, refreshExpiresAt);
    }

    private PasswordVerificationResult VerifyPassword(TAccount account, string password) =>
        string.IsNullOrEmpty(account.PasswordHash)
            ? PasswordVerificationResult.Failed
            : passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);

    private async Task RevokeAllAsync(int accountId, DateTime now, CancellationToken cancellationToken)
    {
        foreach (var token in await refreshTokens.ListAsync(ActiveTokensOf(accountId), cancellationToken))
            token.Revoke(now);
    }
}
