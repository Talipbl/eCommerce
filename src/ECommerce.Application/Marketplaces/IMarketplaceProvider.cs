using ECommerce.Application.Common;
using ECommerce.Marketplaces;
using ECommerce.Ordering;

namespace ECommerce.Application.Marketplaces;

public record PublishListingRequest(
    IReadOnlyList<ListingAttribute>? ProductAttributes = null,
    IReadOnlyList<VariantListingAttributes>? VariantAttributes = null,
    int? DeliveryDuration = null);

public record ListingDto(
    int ProductId,
    ListingStatus Status,
    long? ExternalContentId,
    string? LastBatchRequestId,
    string? StatusMessage,
    DateTime? LastSubmittedAt,
    DateTime? LastCheckedAt,
    IReadOnlyList<ListingAttribute> ProductAttributes,
    IReadOnlyList<VariantListingAttributes> VariantAttributes,
    int? DeliveryDuration);

public sealed record MarketplacePriceStock(string Barcode, int Quantity, decimal SalePrice, decimal ListPrice);

public sealed record MarketplaceOrderLine(long? ExternalLineId, string? Barcode, string? Sku, string ProductName, int Quantity, decimal UnitPrice, int VatRate);

public sealed record MarketplaceOrder(
    string ExternalReference,
    OrderStatus? Status,
    string CustomerEmail,
    string CustomerName,
    OrderAddress Address,
    IReadOnlyList<MarketplaceOrderLine> Lines,
    DateTime PlacedAt,
    string? CargoProvider,
    string? TrackingNumber);

public sealed record MarketplaceOrderPage(IReadOnlyList<MarketplaceOrder> Orders, int TotalPages);

public sealed record MarketplaceLineQuantity(long LineId, int Quantity);

public sealed record MarketplaceStatusCommand(string ExternalReference, MarketplaceOrderAction Action, IReadOnlyList<MarketplaceLineQuantity> Lines, string? InvoiceNumber);

/// <summary>
/// Bir pazaryerinin ortak senkron işlemleri. Senkron servisleri pazaryerinden bağımsızdır; yeni pazaryeri bu arayüzle eklenir.
/// </summary>
public interface IMarketplaceProvider
{
    string Name { get; }

    OrderChannel Channel { get; }

    /// <summary>İçe alınan siparişin yerel numarası bu önek + dış referanstır.</summary>
    string OrderNumberPrefix { get; }

    /// <summary>Tek sipariş sorgusunun kapsayabileceği en uzun aralık.</summary>
    TimeSpan MaxOrderWindow { get; }

    /// <summary>Pazaryerinin sipariş döndürdüğü en eski tarih (şimdiden geriye).</summary>
    TimeSpan MaxOrderLookback { get; }

    int MaxOrderPages { get; }

    int MaxPriceInventoryItems { get; }

    Task<Result<ListingDto>> PublishProductAsync(int productId, PublishListingRequest request, CancellationToken cancellationToken = default);

    Task<MarketplaceResult<bool>> PushPriceInventoryAsync(IReadOnlyList<MarketplacePriceStock> items, CancellationToken cancellationToken = default);

    /// <summary>Son güncellenme tarihine göre artan sırada sipariş sayfası döner.</summary>
    Task<MarketplaceResult<MarketplaceOrderPage>> FetchOrdersAsync(DateTimeOffset from, DateTimeOffset to, int page, CancellationToken cancellationToken = default);

    Task<MarketplaceResult<bool>> UpdateOrderStatusAsync(MarketplaceStatusCommand command, CancellationToken cancellationToken = default);
}

public static class MarketplaceProviders
{
    /// <summary>Retry-After bilgisi gelmediğinde rate limit sonrası bekleme süresi.</summary>
    public static readonly TimeSpan RateLimitPause = TimeSpan.FromSeconds(60);

    public static bool IsTransient(this MarketplaceError error) =>
        error.Kind is MarketplaceErrorKind.Unavailable or MarketplaceErrorKind.Unauthorized;

    public static string Describe(this MarketplaceError error) =>
        error.Messages.Count > 0 ? string.Join("; ", error.Messages) : error.Kind.ToString();
}
