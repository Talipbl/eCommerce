namespace ECommerce.Application.Marketplaces;

public static class MarketplaceProviders
{
    /// <summary>Retry-After bilgisi gelmediğinde rate limit sonrası bekleme süresi.</summary>
    public static readonly TimeSpan RateLimitPause = TimeSpan.FromSeconds(60);

    public static bool IsTransient(this MarketplaceError error) =>
        error.Kind is MarketplaceErrorKind.Unavailable or MarketplaceErrorKind.Unauthorized;

    public static string Describe(this MarketplaceError error) =>
        error.Messages.Count > 0 ? string.Join("; ", error.Messages) : error.Kind.ToString();
}
