namespace ECommerce.Accounts;

/// <summary>
/// Parola ile giriş yapan hesap (yönetici veya müşteri). Kilitleme kuralları hesabın kendisindedir.
/// </summary>
public interface IAccount
{
    int Id { get; }
    string DisplayName { get; }
    string Role { get; }
    string PasswordHash { get; set; }
    bool IsActive { get; }
    bool IsLockedOut(DateTime now);
    void RegisterFailedLogin(DateTime now, int maxAttempts, TimeSpan lockoutDuration);
    void RegisterSuccessfulLogin(DateTime now);
}

public interface IRefreshToken<TSelf> where TSelf : IRefreshToken<TSelf>
{
    int AccountId { get; }
    DateTime? RevokedAt { get; }
    bool IsActive(DateTime now);
    void Revoke(DateTime now, string? replacedByTokenHash = null);
    static abstract TSelf Create(int accountId, string tokenHash, DateTime expiresAt);
}
