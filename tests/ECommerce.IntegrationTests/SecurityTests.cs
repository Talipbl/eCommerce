using ECommerce.IntegrationTests.Infrastructure;
using System.Net;

namespace ECommerce.IntegrationTests;

public class SecurityTests
{
    private const string UnreachableDatabase = "Server=127.0.0.1,1;Database=None;User Id=sa;Password=x;TrustServerCertificate=True;Connect Timeout=1";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Responses_include_security_headers()
    {
        await using var factory = new ApiFactory(UnreachableDatabase);

        var response = await factory.CreateClient().GetAsync("/health/live", Ct);

        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        response.Headers.GetValues("X-Frame-Options").ShouldContain("DENY");
        response.Headers.GetValues("Referrer-Policy").ShouldContain("no-referrer");
        response.Headers.GetValues("Content-Security-Policy").Single().ShouldContain("default-src 'none'");
    }

    [Fact]
    public async Task Login_is_rate_limited_per_client()
    {
        await using var factory = new ApiFactory(UnreachableDatabase, new Dictionary<string, string?>
        {
            ["RateLimiting:Auth:PermitLimit"] = "2"
        });
        var client = factory.CreateClient();

        // Validasyon hatası veritabanına gitmeden döner; limit sayacı yine de artar.
        for (var i = 0; i < 2; i++)
            (await AuthClient.LoginAsync(client, "", "")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var limited = await AuthClient.LoginAsync(client, "", "");
        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        limited.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Cors_allows_only_configured_frontend_origin_with_credentials()
    {
        await using var factory = new ApiFactory(UnreachableDatabase, new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://panel.example.com"
        });
        var client = factory.CreateClient();

        var allowed = await client.SendAsync(Preflight("https://panel.example.com"), Ct);
        allowed.Headers.GetValues("Access-Control-Allow-Origin").ShouldContain("https://panel.example.com");
        allowed.Headers.GetValues("Access-Control-Allow-Credentials").ShouldContain("true");

        var denied = await client.SendAsync(Preflight("https://evil.example.com"), Ct);
        denied.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Fact]
    public async Task Missing_signing_key_fails_startup()
    {
        await using var factory = new ApiFactory(UnreachableDatabase, new Dictionary<string, string?> { ["Jwt:SigningKey"] = "kisa" });

        Should.Throw<Exception>(() => factory.CreateClient());
    }

    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        return request;
    }
}
