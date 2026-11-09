namespace ECommerce.Marketplaces;

/// <summary>
/// Stok/fiyat outbox kaydı: varyant değiştiğinde aynı kayıtta yazılır, arka plan işi pazaryerine gönderir.
/// </summary>
public class MarketplaceInventoryChange : MarketplaceOutboxMessage
{
    public int VariantId { get; private set; }

    public static MarketplaceInventoryChange For(int variantId) => new() { VariantId = variantId };
}
