using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;
using System.Net;

namespace ECommerce.AdminPanel.Tests.Infrastructure;

public class ApiAuthClientTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ApiAuthClient Client(FakeApi api) => new(new HttpClient(api) { BaseAddress = new Uri("http://api.test/") });

    [Fact]
    public async Task Login_reads_access_token_from_body_and_refresh_token_from_cookie()
    {
        var expiresAt = new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
        var api = new FakeApi().On("POST", "/api/v1/auth/login", _ => FakeApi.Tokens("access", expiresAt, "refresh"));

        var tokens = await Client(api).LoginAsync("admin", "Parola123!", Ct);

        tokens.ShouldBe(new ApiTokens("access", expiresAt, "refresh"));
        api.Requests.Single().Body.ShouldBe("""{"userName":"admin","password":"Parola123!"}""");
    }

    [Fact]
    public async Task Login_failure_keeps_api_problem_for_the_form()
    {
        var api = new FakeApi().On("POST", "/api/v1/auth/login", _ => FakeApi.Problem(HttpStatusCode.Unauthorized, "Kullanıcı adı veya parola hatalı."));

        var exception = await Should.ThrowAsync<ApiException>(() => Client(api).LoginAsync("admin", "yanlis", Ct));

        exception.StatusCode.ShouldBe(401);
        ApiProblem.From(exception).Title.ShouldBe("Kullanıcı adı veya parola hatalı.");
    }

    [Fact]
    public async Task Login_without_refresh_cookie_is_an_error()
    {
        var api = new FakeApi().On("POST", "/api/v1/auth/login", _ =>
            FakeApi.Json(HttpStatusCode.OK, new { accessToken = "access", expiresAt = DateTimeOffset.UtcNow }));

        await Should.ThrowAsync<InvalidOperationException>(() => Client(api).LoginAsync("admin", "Parola123!", Ct));
    }

    [Fact]
    public async Task Refresh_sends_refresh_token_as_cookie_header()
    {
        var api = new FakeApi().On("POST", "/api/v1/auth/refresh", _ => FakeApi.Tokens("access-2", DateTimeOffset.UtcNow.AddMinutes(15), "refresh-2"));

        var tokens = await Client(api).RefreshAsync("refresh-1", Ct);

        tokens!.RefreshToken.ShouldBe("refresh-2");
        api.Requests.Single().Cookie.ShouldBe("ecommerce_refresh=refresh-1");
    }

    [Fact]
    public async Task Rejected_refresh_returns_null()
    {
        var api = new FakeApi().On("POST", "/api/v1/auth/refresh", _ => FakeApi.Problem(HttpStatusCode.Unauthorized, "Oturumun süresi doldu."));

        (await Client(api).RefreshAsync("refresh-1", Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Logout_sends_refresh_token_as_cookie_header()
    {
        var api = FakeApi.Admin();

        await Client(api).LogoutAsync("refresh-1", Ct);

        api.Requests.Single().Cookie.ShouldBe("ecommerce_refresh=refresh-1");
    }
}
