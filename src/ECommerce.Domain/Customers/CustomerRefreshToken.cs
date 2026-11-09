using ECommerce.Accounts;
using ECommerce.Core.Domain;

namespace ECommerce.Customers;

public class CustomerRefreshToken : AggregateRoot<int>, IRefreshToken<CustomerRefreshToken>
{
    public int CustomerId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    int IRefreshToken<CustomerRefreshToken>.AccountId => CustomerId;

    public static CustomerRefreshToken Create(int accountId, string tokenHash, DateTime expiresAt) =>
        new() { CustomerId = accountId, TokenHash = tokenHash, ExpiresAt = expiresAt };

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;

    public void Revoke(DateTime now, string? replacedByTokenHash = null)
    {
        RevokedAt ??= now;
        ReplacedByTokenHash ??= replacedByTokenHash;
    }
}
