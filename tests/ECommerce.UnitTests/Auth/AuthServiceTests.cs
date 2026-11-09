using ECommerce.Application.Auth;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.UnitTests.Auth;

public class AuthServiceTests : IAsyncLifetime
{
    private readonly AuthTestHarness _h = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Login_with_valid_credentials_issues_tokens_and_stores_only_hash()
    {
        var admin = await _h.AddAdminAsync();

        var result = await _h.Service.LoginAsync(new LoginRequest("admin", AuthTestHarness.Password), Ct);

        result.Succeeded.ShouldBeTrue();
        var tokens = result.Value!;
        tokens.AccessToken.ShouldNotBeNullOrWhiteSpace();
        tokens.AccessTokenExpiresAt.ShouldBe(_h.Clock.GetUtcNow().UtcDateTime.AddMinutes(15));
        tokens.RefreshTokenExpiresAt.ShouldBe(_h.Clock.GetUtcNow().UtcDateTime.AddDays(7));

        var stored = await _h.Context.AdminRefreshTokens.SingleAsync(Ct);
        stored.AdminId.ShouldBe(admin.Id);
        stored.TokenHash.ShouldBe(_h.Tokens.HashRefreshToken(tokens.RefreshToken));
        stored.TokenHash.ShouldNotBe(tokens.RefreshToken);
        admin.LastLoginDate.ShouldBe(_h.Clock.GetUtcNow().UtcDateTime);
    }

    [Theory]
    [InlineData("admin", "yanlis-parola")]
    [InlineData("olmayan", AuthTestHarness.Password)]
    public async Task Login_with_invalid_credentials_fails(string userName, string password)
    {
        await _h.AddAdminAsync();

        var result = await _h.Service.LoginAsync(new LoginRequest(userName, password), Ct);

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe(AuthError.InvalidCredentials);
        (await _h.Context.AdminRefreshTokens.CountAsync(Ct)).ShouldBe(0);
    }

    [Fact]
    public async Task Login_of_inactive_admin_fails()
    {
        await _h.AddAdminAsync(isActive: false);

        var result = await _h.Service.LoginAsync(new LoginRequest("admin", AuthTestHarness.Password), Ct);

        result.Error.ShouldBe(AuthError.InvalidCredentials);
    }

    [Fact]
    public async Task Repeated_failures_lock_the_account_even_for_correct_password()
    {
        var admin = await _h.AddAdminAsync();

        for (var i = 0; i < AuthTestHarness.Auth.MaxFailedLoginAttempts; i++)
            await _h.Service.LoginAsync(new LoginRequest("admin", "yanlis"), Ct);

        var locked = await _h.Service.LoginAsync(new LoginRequest("admin", AuthTestHarness.Password), Ct);
        locked.Error.ShouldBe(AuthError.LockedOut);
        admin.IsLockedOut(_h.Clock.GetUtcNow().UtcDateTime).ShouldBeTrue();

        _h.Clock.Advance(TimeSpan.FromMinutes(AuthTestHarness.Auth.LockoutMinutes + 1));
        var afterLockout = await _h.Service.LoginAsync(new LoginRequest("admin", AuthTestHarness.Password), Ct);
        afterLockout.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Refresh_rotates_token()
    {
        await _h.AddAdminAsync();
        var login = (await _h.Service.LoginAsync(new LoginRequest("admin", AuthTestHarness.Password), Ct)).Value!;

        var refreshed = await _h.Service.RefreshAsync(login.RefreshToken, Ct);

        refreshed.Succeeded.ShouldBeTrue();
        refreshed.Value!.RefreshToken.ShouldNotBe(login.RefreshToken);
        var old = await _h.Context.AdminRefreshTokens.SingleAsync(t => t.TokenHash == _h.Tokens.HashRefreshToken(login.RefreshToken), Ct);
        old.RevokedAt.ShouldNotBeNull();
        old.ReplacedByTokenHash.ShouldBe(_h.Tokens.HashRefreshToken(refreshed.Value.RefreshToken));
    }

    [Fact]
    public async Task Reusing_a_rotated_token_revokes_all_tokens_of_admin()
    {
        await _h.AddAdminAsync();
        var login = (await _h.Service.LoginAsync(new LoginRequest("admin", AuthTestHarness.Password), Ct)).Value!;
        var rotated = (await _h.Service.RefreshAsync(login.RefreshToken, Ct)).Value!;

        var reuse = await _h.Service.RefreshAsync(login.RefreshToken, Ct);

        reuse.Error.ShouldBe(AuthError.InvalidRefreshToken);
        (await _h.Service.RefreshAsync(rotated.RefreshToken, Ct)).Error.ShouldBe(AuthError.InvalidRefreshToken);
    }

    [Fact]
    public async Task Expired_or_unknown_refresh_token_fails()
    {
        await _h.AddAdminAsync();
        var login = (await _h.Service.LoginAsync(new LoginRequest("admin", AuthTestHarness.Password), Ct)).Value!;

        (await _h.Service.RefreshAsync("bilinmeyen-token", Ct)).Error.ShouldBe(AuthError.InvalidRefreshToken);

        _h.Clock.Advance(TimeSpan.FromDays(AuthTestHarness.Auth.RefreshTokenDays));
        (await _h.Service.RefreshAsync(login.RefreshToken, Ct)).Error.ShouldBe(AuthError.InvalidRefreshToken);
    }

    [Fact]
    public async Task Logout_revokes_refresh_token()
    {
        await _h.AddAdminAsync();
        var login = (await _h.Service.LoginAsync(new LoginRequest("admin", AuthTestHarness.Password), Ct)).Value!;

        await _h.Service.LogoutAsync(login.RefreshToken, Ct);

        (await _h.Service.RefreshAsync(login.RefreshToken, Ct)).Error.ShouldBe(AuthError.InvalidRefreshToken);
    }

    [Fact]
    public async Task Change_password_requires_current_password()
    {
        var admin = await _h.AddAdminAsync();

        var result = await _h.Service.ChangePasswordAsync(admin.Id, new ChangePasswordRequest("yanlis", "Yeni-Parola-456"), Ct);

        result.Error.ShouldBe(AuthError.InvalidCredentials);
    }

    [Fact]
    public async Task Change_password_updates_hash_and_revokes_sessions()
    {
        var admin = await _h.AddAdminAsync();
        var login = (await _h.Service.LoginAsync(new LoginRequest("admin", AuthTestHarness.Password), Ct)).Value!;

        var result = await _h.Service.ChangePasswordAsync(admin.Id, new ChangePasswordRequest(AuthTestHarness.Password, "Yeni-Parola-456"), Ct);

        result.Succeeded.ShouldBeTrue();
        (await _h.Service.RefreshAsync(login.RefreshToken, Ct)).Error.ShouldBe(AuthError.InvalidRefreshToken);
        (await _h.Service.LoginAsync(new LoginRequest("admin", AuthTestHarness.Password), Ct)).Succeeded.ShouldBeFalse();
        (await _h.Service.LoginAsync(new LoginRequest("admin", "Yeni-Parola-456"), Ct)).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Get_profile_returns_admin_details()
    {
        var admin = await _h.AddAdminAsync();

        var profile = await _h.Service.GetProfileAsync(admin.Id, Ct);

        profile.ShouldNotBeNull();
        profile.UserName.ShouldBe("admin");
        profile.Role.ShouldBe("Admin");
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _h.DisposeAsync();
}
