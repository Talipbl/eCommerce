using ECommerce.Application.Common;
using ECommerce.Ordering;

namespace ECommerce.Application.Marketplaces;

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
