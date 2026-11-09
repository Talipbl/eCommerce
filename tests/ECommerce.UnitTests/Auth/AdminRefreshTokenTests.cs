using ECommerce.Admin;

namespace ECommerce.UnitTests.Auth;

public class AdminRefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void New_token_is_active_until_expiry()
    {
        var token = AdminRefreshToken.Create(adminId: 1, tokenHash: "hash", expiresAt: Now.AddDays(7));

        token.IsActive(Now).ShouldBeTrue();
        token.IsActive(Now.AddDays(7)).ShouldBeFalse();
    }

    [Fact]
    public void Revoked_token_is_not_active_and_keeps_replacement()
    {
        var token = AdminRefreshToken.Create(adminId: 1, tokenHash: "hash", expiresAt: Now.AddDays(7));

        token.Revoke(Now, replacedByTokenHash: "next");

        token.IsActive(Now).ShouldBeFalse();
        token.RevokedAt.ShouldBe(Now);
        token.ReplacedByTokenHash.ShouldBe("next");
    }
}
