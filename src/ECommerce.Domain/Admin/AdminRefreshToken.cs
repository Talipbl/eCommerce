using ECommerce.Accounts;
using ECommerce.Core.Domain;

namespace ECommerce.Admin;

/// <summary>
/// Yöneticinin refresh token kaydı. Token'ın kendisi değil, SHA-256 özeti saklanır.
/// </summary>
public class AdminRefreshToken : AggregateRoot<int>, IRefreshToken<AdminRefreshToken>
{
    int IRefreshToken<AdminRefreshToken>.AccountId => AdminId;

    public int AdminId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public virtual DefAdmin Admin { get; private set; } = null!;

    public static AdminRefreshToken Create(int adminId, string tokenHash, DateTime expiresAt) =>
        new() { AdminId = adminId, TokenHash = tokenHash, ExpiresAt = expiresAt };

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;

    public void Revoke(DateTime now, string? replacedByTokenHash = null)
    {
        RevokedAt ??= now;
        ReplacedByTokenHash ??= replacedByTokenHash;
    }
}
