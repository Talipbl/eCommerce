using ECommerce.AdminPanel.Api;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;

namespace ECommerce.AdminPanel.Infrastructure;

/// <summary>
/// Giriş, yenileme ve çıkış uçları refresh cookie'si taşıdığı için üretilen client yerine burada elle çağrılır.
/// Cookie container kullanılmaz: refresh token <c>Set-Cookie</c>'dan okunur, <c>Cookie</c> başlığıyla geri gönderilir.
/// </summary>
internal sealed class ApiAuthClient(HttpClient http) : IApiAuthClient
{
    public const string RefreshCookieName = "ecommerce_refresh";

    public async Task<ApiTokens> LoginAsync(string userName, string password, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync("api/v1/auth/login", new LoginRequest { UserName = userName, Password = password }, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await ToApiExceptionAsync(response, cancellationToken);

        return await ReadTokensAsync(response, cancellationToken);
    }

    public async Task<ApiTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var response = await http.SendAsync(WithRefreshCookie(HttpMethod.Post, "api/v1/auth/refresh", refreshToken), cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return null;
        if (!response.IsSuccessStatusCode)
            throw await ToApiExceptionAsync(response, cancellationToken);

        return await ReadTokensAsync(response, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var response = await http.SendAsync(WithRefreshCookie(HttpMethod.Post, "api/v1/auth/logout", refreshToken), cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await ToApiExceptionAsync(response, cancellationToken);
    }

    private static HttpRequestMessage WithRefreshCookie(HttpMethod method, string path, string refreshToken)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(HeaderNames.Cookie, $"{RefreshCookieName}={refreshToken}");
        return request;
    }

    private static async Task<ApiTokens> ReadTokensAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("API token cevabı boş döndü.");

        var cookies = response.Headers.TryGetValues(HeaderNames.SetCookie, out var values) ? values.ToList() : [];
        var refreshCookie = SetCookieHeaderValue.ParseList(cookies).FirstOrDefault(c => c.Name == RefreshCookieName && !string.IsNullOrEmpty(c.Value.Value))
            ?? throw new InvalidOperationException("API refresh cookie'si döndürmedi.");

        return new ApiTokens(body.AccessToken, body.ExpiresAt, refreshCookie.Value.Value!);
    }

    private static async Task<ApiException> ToApiExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var headers = response.Headers.Concat(response.Content.Headers).ToDictionary(h => h.Key, h => h.Value);
        return new ApiException($"API {(int)response.StatusCode} döndü.", (int)response.StatusCode, body, headers, null);
    }
}
