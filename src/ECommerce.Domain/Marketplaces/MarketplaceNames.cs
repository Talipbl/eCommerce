using ECommerce.Ordering;

namespace ECommerce.Marketplaces;

public static class MarketplaceNames
{
    public const string Trendyol = "Trendyol";

    public static string? ForChannel(OrderChannel channel) => channel switch
    {
        OrderChannel.Trendyol => Trendyol,
        _ => null
    };
}
