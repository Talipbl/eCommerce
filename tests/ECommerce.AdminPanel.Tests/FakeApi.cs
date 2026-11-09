using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ECommerce.AdminPanel.Tests;

public sealed record RecordedRequest(HttpMethod Method, string Path, string Query, string? Bearer, string? Cookie, string? ForwardedFor, string? Body, string? ContentType);

/// <summary>API'yi taklit eden primary handler; rotası tanımlanmayan istekler 404 döner.</summary>
public sealed class FakeApi : HttpMessageHandler
{
    public const string AccessToken = "access-1";
    public const string RefreshToken = "refresh-1";

    private readonly ConcurrentDictionary<string, Func<RecordedRequest, Task<HttpResponseMessage>>> _routes = new();

    public ConcurrentQueue<RecordedRequest> Requests { get; } = new();

    /// <summary>Giriş ve çıkışı başarılı cevaplayan API.</summary>
    public static FakeApi Admin() => new FakeApi()
        .On("POST", "/api/v1/auth/login", _ => Tokens(AccessToken, DateTimeOffset.UtcNow.AddMinutes(15), RefreshToken))
        .On("POST", "/api/v1/auth/logout", _ => new HttpResponseMessage(HttpStatusCode.NoContent));

    public FakeApi On(string method, string path, Func<RecordedRequest, HttpResponseMessage> respond) =>
        On(method, path, request => Task.FromResult(respond(request)));

    public FakeApi On(string method, string path, Func<RecordedRequest, Task<HttpResponseMessage>> respond)
    {
        _routes[$"{method} {path}"] = respond;
        return this;
    }

    public IEnumerable<RecordedRequest> To(string path) => Requests.Where(r => r.Path == path);

    public static HttpResponseMessage Tokens(string accessToken, DateTimeOffset expiresAt, string refreshToken)
    {
        var response = Json(HttpStatusCode.OK, new { accessToken, expiresAt, tokenType = "Bearer" });
        response.Headers.Add("Set-Cookie",
            $"ecommerce_refresh={refreshToken}; expires={expiresAt.AddDays(7):R}; path=/api/v1/auth; secure; samesite=strict; httponly");
        return response;
    }

    public static HttpResponseMessage Problem(HttpStatusCode status, string title) =>
        Json(status, new { title, status = (int)status });

    public static HttpResponseMessage Json(HttpStatusCode status, object body) => new(status)
    {
        Content = new StringContent(JsonSerializer.Serialize(body, JsonSerializerOptions.Web), Encoding.UTF8, "application/json")
    };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var recorded = new RecordedRequest(
            request.Method,
            request.RequestUri!.AbsolutePath,
            request.RequestUri.Query,
            request.Headers.Authorization?.Parameter,
            Header(request, "Cookie"),
            Header(request, "X-Forwarded-For"),
            request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
            request.Content?.Headers.ContentType?.MediaType);
        Requests.Enqueue(recorded);

        return _routes.TryGetValue($"{request.Method} {recorded.Path}", out var respond)
            ? await respond(recorded)
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static string? Header(HttpRequestMessage request, string name) =>
        request.Headers.TryGetValues(name, out var values) ? string.Join(", ", values) : null;
}
