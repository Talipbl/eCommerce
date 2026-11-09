using ECommerce.Admin;

namespace ECommerce.UnitTests.Auth;

public class DefAdminTests
{
    private static readonly DateTime Now = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Lockout = TimeSpan.FromMinutes(15);

    [Fact]
    public void Account_is_locked_after_max_failed_attempts()
    {
        var admin = new DefAdmin();

        for (var i = 0; i < 4; i++)
            admin.RegisterFailedLogin(Now, maxAttempts: 5, Lockout);
        admin.IsLockedOut(Now).ShouldBeFalse();

        admin.RegisterFailedLogin(Now, maxAttempts: 5, Lockout);

        admin.IsLockedOut(Now).ShouldBeTrue();
        admin.LockoutEndDate.ShouldBe(Now.Add(Lockout));
        admin.FailedLoginCount.ShouldBe(0);
    }

    [Fact]
    public void Lockout_expires_after_duration()
    {
        var admin = new DefAdmin();
        for (var i = 0; i < 5; i++)
            admin.RegisterFailedLogin(Now, maxAttempts: 5, Lockout);

        admin.IsLockedOut(Now.Add(Lockout).AddSeconds(1)).ShouldBeFalse();
    }

    [Fact]
    public void Successful_login_resets_failures_and_sets_last_login()
    {
        var admin = new DefAdmin();
        admin.RegisterFailedLogin(Now, maxAttempts: 5, Lockout);

        admin.RegisterSuccessfulLogin(Now);

        admin.FailedLoginCount.ShouldBe(0);
        admin.LastLoginDate.ShouldBe(Now);
    }
}
