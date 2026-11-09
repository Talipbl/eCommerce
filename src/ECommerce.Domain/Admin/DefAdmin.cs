using ECommerce.Accounts;
using ECommerce.Core.Domain;

namespace ECommerce.Admin;

public partial class DefAdmin : AggregateRoot<int>, IAccount
{
    public string DisplayName => UserName;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public DateTime RegisterDate { get; set; }

    public DateTime? LastLoginDate { get; private set; }

    public string MailAdress { get; set; } = null!;

    public string? OtpsetupCode { get; set; }

    public bool? IsEnable2Fa { get; set; }

    public bool IsActive { get; set; } = true;

    public string Role { get; set; } = AdminRoles.Admin;

    public string PasswordHash { get; set; } = string.Empty;

    public int FailedLoginCount { get; private set; }

    public DateTime? LockoutEndDate { get; private set; }

    public virtual ICollection<AdminRefreshToken> RefreshTokens { get; } = new List<AdminRefreshToken>();

    public bool IsLockedOut(DateTime now) => LockoutEndDate > now;

    public void RegisterFailedLogin(DateTime now, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginCount++;
        if (FailedLoginCount < maxAttempts)
            return;

        LockoutEndDate = now.Add(lockoutDuration);
        FailedLoginCount = 0;
    }

    public void RegisterSuccessfulLogin(DateTime now)
    {
        FailedLoginCount = 0;
        LockoutEndDate = null;
        LastLoginDate = now;
    }
}
