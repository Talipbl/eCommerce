using ECommerce.AdminPanel.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Security.Claims;

namespace ECommerce.AdminPanel.Tests.Infrastructure;

public class ApiTokenHandlerTests
{
    private const string Path = "/api/v1/orders";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly AdminSessionStore _sessions = new(new MemoryCache(new MemoryCacheOptions()));
    private readonly DefaultHttpContext _httpContext = new();

    private AdminSession SignIn(TimeSpan accessTokenLifetime)
    {
        var session = _sessions.Create("admin", new ApiTokens("a1", DateTimeOffset.UtcNow + accessTokenLifetime, "r1"));
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(AdminSessionStore.SessionIdClaim, session.Id)], "test"));
        return session;
    }

    private HttpMessageInvoker Invoker(FakeApi api, IApiAuthClient auth) =>
        new(new ApiTokenHandler(new HttpContextAccessor { HttpContext = _httpContext }, _sessions, auth, TimeProvider.System) { InnerHandler = api });

    private static FakeApi AcceptOnly(string accessToken) => new FakeApi().On("POST", Path, request =>
        new HttpResponseMessage(request.Bearer == accessToken ? HttpStatusCode.OK : HttpStatusCode.Unauthorized));

    private static HttpRequestMessage Post(string body = "{}") =>
        new(HttpMethod.Post, $"http://api.test{Path}") { Content = new StringContent(body) };

    [Fact]
    public async Task Adds_session_access_token_as_bearer()
    {
        SignIn(TimeSpan.FromMinutes(10));
        var api = AcceptOnly("a1");
        var auth = FakeAuthClient.Rotating();

        var response = await Invoker(api, auth).SendAsync(Post(), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        auth.RefreshCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Refreshes_before_sending_when_token_expires_soon()
    {
        SignIn(ApiTokenHandler.RefreshBeforeExpiry - TimeSpan.FromSeconds(1));
        var api = AcceptOnly("r1-access");
        var auth = FakeAuthClient.Rotating();

        var response = await Invoker(api, auth).SendAsync(Post(), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        api.Requests.Single().Bearer.ShouldBe("r1-access");
        auth.RefreshCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Unauthorized_response_is_retried_once_with_refreshed_token_and_same_body()
    {
        SignIn(TimeSpan.FromMinutes(10));
        var api = AcceptOnly("r1-access");

        var response = await Invoker(api, FakeAuthClient.Rotating()).SendAsync(Post("""{"note":"kargo"}"""), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        api.Requests.Select(r => (r.Bearer, r.Body)).ShouldBe([("a1", """{"note":"kargo"}"""), ("r1-access", """{"note":"kargo"}""")]);
    }

    [Fact]
    public async Task Parallel_requests_with_expired_token_refresh_once()
    {
        SignIn(TimeSpan.FromMinutes(10));
        var api = AcceptOnly("r1-access");
        var auth = FakeAuthClient.Rotating(blocked: true);
        var invoker = Invoker(api, auth);

        var requests = Enumerable.Range(0, 5).Select(_ => invoker.SendAsync(Post(), Ct)).ToArray();
        auth.Release();
        var responses = await Task.WhenAll(requests);

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.OK);
        auth.RefreshCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Rejected_refresh_ends_session()
    {
        var session = SignIn(TimeSpan.FromMinutes(10));
        var api = AcceptOnly("never");

        await Should.ThrowAsync<AdminSessionExpiredException>(() => Invoker(api, new FakeAuthClient(_ => null)).SendAsync(Post(), Ct));

        _sessions.Find(_httpContext.User).ShouldBeNull();
        api.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Unauthorized_after_refresh_ends_session()
    {
        SignIn(TimeSpan.FromMinutes(10));
        var api = AcceptOnly("never");

        await Should.ThrowAsync<AdminSessionExpiredException>(() => Invoker(api, FakeAuthClient.Rotating()).SendAsync(Post(), Ct));

        _sessions.Find(_httpContext.User).ShouldBeNull();
        api.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Request_without_server_session_is_not_sent()
    {
        var api = AcceptOnly("a1");

        await Should.ThrowAsync<AdminSessionExpiredException>(() => Invoker(api, FakeAuthClient.Rotating()).SendAsync(Post(), Ct));

        api.Requests.ShouldBeEmpty();
    }
}
