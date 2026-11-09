using ECommerce.Admin;
using ECommerce.Core.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ECommerce.IntegrationTests.Infrastructure;

public record TokenResponse(string AccessToken, DateTime ExpiresAt, string TokenType);

public static class AuthClient
{
    public const string CookieName = "ecommerce_refresh";

    public static async Task CreateAdminAsync(ApiFactory factory, string userName, string password)
    {
        using var scope = factory.Services.CreateScope();
        var admins = scope.ServiceProvider.GetRequiredService<IRepository<DefAdmin>>();
        var admin = new DefAdmin { UserName = userName, FirstName = "Test", LastName = "Admin", MailAdress = $"{userName}@example.com", RegisterDate = DateTime.UtcNow };
        admin.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<DefAdmin>>().HashPassword(admin, password);
        admins.Add(admin);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
    }

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string userName, string password) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { userName, password }, TestContext.Current.CancellationToken);

    public static string? GetRefreshCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith(CookieName + "=", StringComparison.Ordinal))
            : null;

    public static string GetRefreshCookieValue(HttpResponseMessage response) =>
        GetRefreshCookie(response)!.Split(';')[0][(CookieName.Length + 1)..];

    public static HttpRequestMessage WithRefreshCookie(HttpMethod method, string url, string cookieValue)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Cookie", $"{CookieName}={cookieValue}");
        return request;
    }

    public static HttpRequestMessage WithBearer(HttpMethod method, string url, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return request;
    }
}
