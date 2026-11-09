using ECommerce.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace ECommerce.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public class AuthEndpointsTests : IAsyncLifetime
{
    private const string SeedUser = "seedadmin";
    private const string SeedPassword = "Seed-Parola-123";

    private readonly SqlServerFixture _database;
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public AuthEndpointsTests(SqlServerFixture database) => _database = database;

    public async ValueTask InitializeAsync()
    {
        Assert.SkipWhen(_database.UnavailableReason is not null, _database.UnavailableReason ?? string.Empty);

        _factory = new ApiFactory(_database.ConnectionString, new Dictionary<string, string?>
        {
            ["Seed:Admin:UserName"] = SeedUser,
            ["Seed:Admin:Password"] = SeedPassword,
            ["Seed:Admin:Email"] = "seed@example.com"
        });
        _client = _factory.CreateClient(new() { HandleCookies = false });
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Seeded_admin_can_login_and_receives_secure_refresh_cookie()
    {
        var response = await AuthClient.LoginAsync(_client, SeedUser, SeedPassword);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>(Ct);
        body.ShouldNotBeNull().AccessToken.ShouldNotBeNullOrWhiteSpace();
        body.TokenType.ShouldBe("Bearer");

        var cookie = AuthClient.GetRefreshCookie(response).ShouldNotBeNull().ToLowerInvariant();
        cookie.ShouldContain("httponly");
        cookie.ShouldContain("secure");
        cookie.ShouldContain("samesite=strict");
        cookie.ShouldContain("path=/api/v1/auth");
    }

    [Fact]
    public async Task Invalid_credentials_return_generic_401_problem()
    {
        var response = await AuthClient.LoginAsync(_client, "olmayan-kullanici", "Yanlis-Parola-1");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        AuthClient.GetRefreshCookie(response).ShouldBeNull();
    }

    [Fact]
    public async Task Empty_login_request_returns_validation_problem()
    {
        var response = await AuthClient.LoginAsync(_client, "", "");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync(Ct);
        body.ShouldContain("UserName");
        body.ShouldContain("Password");
    }

    [Fact]
    public async Task Me_requires_token_and_returns_profile()
    {
        (await _client.GetAsync("/api/v1/auth/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var login = await (await AuthClient.LoginAsync(_client, SeedUser, SeedPassword)).Content.ReadFromJsonAsync<TokenResponse>(Ct);
        var response = await _client.SendAsync(AuthClient.WithBearer(HttpMethod.Get, "/api/v1/auth/me", login!.AccessToken), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain(SeedUser);
    }

    [Fact]
    public async Task Tampered_token_is_rejected()
    {
        var login = await (await AuthClient.LoginAsync(_client, SeedUser, SeedPassword)).Content.ReadFromJsonAsync<TokenResponse>(Ct);

        var response = await _client.SendAsync(AuthClient.WithBearer(HttpMethod.Get, "/api/v1/auth/me", login!.AccessToken + "x"), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_rotates_cookie_and_old_cookie_cannot_be_reused()
    {
        var login = await AuthClient.LoginAsync(_client, SeedUser, SeedPassword);
        var firstCookie = AuthClient.GetRefreshCookieValue(login);

        var refreshed = await _client.SendAsync(AuthClient.WithRefreshCookie(HttpMethod.Post, "/api/v1/auth/refresh", firstCookie), Ct);
        refreshed.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondCookie = AuthClient.GetRefreshCookieValue(refreshed);
        secondCookie.ShouldNotBe(firstCookie);

        var reuse = await _client.SendAsync(AuthClient.WithRefreshCookie(HttpMethod.Post, "/api/v1/auth/refresh", firstCookie), Ct);
        reuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var afterReuse = await _client.SendAsync(AuthClient.WithRefreshCookie(HttpMethod.Post, "/api/v1/auth/refresh", secondCookie), Ct);
        afterReuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_without_cookie_returns_401()
    {
        var response = await _client.PostAsync("/api/v1/auth/refresh", null, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_revokes_session_and_clears_cookie()
    {
        var login = await AuthClient.LoginAsync(_client, SeedUser, SeedPassword);
        var cookie = AuthClient.GetRefreshCookieValue(login);

        var logout = await _client.SendAsync(AuthClient.WithRefreshCookie(HttpMethod.Post, "/api/v1/auth/logout", cookie), Ct);
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        AuthClient.GetRefreshCookie(logout).ShouldNotBeNull().ShouldContain("expires=Thu, 01 Jan 1970");

        var refresh = await _client.SendAsync(AuthClient.WithRefreshCookie(HttpMethod.Post, "/api/v1/auth/refresh", cookie), Ct);
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Change_password_validates_rules_and_ends_sessions()
    {
        const string user = "parola-degistiren";
        await AuthClient.CreateAdminAsync(_factory, user, "Ilk-Parola-123");
        var login = await AuthClient.LoginAsync(_client, user, "Ilk-Parola-123");
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>(Ct))!.AccessToken;
        var cookie = AuthClient.GetRefreshCookieValue(login);

        var weak = await _client.SendAsync(AuthClient.WithBearer(HttpMethod.Post, "/api/v1/auth/change-password", token,
            new { currentPassword = "Ilk-Parola-123", newPassword = "kisa" }), Ct);
        weak.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var wrongCurrent = await _client.SendAsync(AuthClient.WithBearer(HttpMethod.Post, "/api/v1/auth/change-password", token,
            new { currentPassword = "Yanlis-Parola-1", newPassword = "Yeni-Parola-456" }), Ct);
        wrongCurrent.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var changed = await _client.SendAsync(AuthClient.WithBearer(HttpMethod.Post, "/api/v1/auth/change-password", token,
            new { currentPassword = "Ilk-Parola-123", newPassword = "Yeni-Parola-456" }), Ct);
        changed.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await _client.SendAsync(AuthClient.WithRefreshCookie(HttpMethod.Post, "/api/v1/auth/refresh", cookie), Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuthClient.LoginAsync(_client, user, "Yeni-Parola-456")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
