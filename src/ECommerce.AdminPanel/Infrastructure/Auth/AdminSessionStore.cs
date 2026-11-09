using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Security.Cryptography;

namespace ECommerce.AdminPanel.Infrastructure;

/// <summary>Oturumları bellekte tutar; panel yeniden başlarsa yönetici tekrar giriş yapar.</summary>
public sealed class AdminSessionStore(IMemoryCache cache)
{
    public const string SessionIdClaim = "sid";
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromHours(8);

    public AdminSession Create(string userName, ApiTokens tokens)
    {
        var session = new AdminSession(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), userName, tokens);
        cache.Set(Key(session.Id), session, new MemoryCacheEntryOptions { SlidingExpiration = IdleTimeout });
        return session;
    }

    public AdminSession? Find(ClaimsPrincipal? user) =>
        user?.FindFirst(SessionIdClaim)?.Value is { Length: > 0 } id ? cache.Get<AdminSession>(Key(id)) : null;

    public void Remove(string sessionId) => cache.Remove(Key(sessionId));

    private static string Key(string sessionId) => $"admin-session:{sessionId}";
}
