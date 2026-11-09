using Trendyol.Client.Orders;

namespace ECommerce.Application.Marketplaces.Trendyol;

public interface ITrendyolOrderService
{
    Task<MarketplaceResult<ShipmentPackagePage>> GetPackagesAsync(OrderPackagesQuery query, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<bool>> StartPickingAsync(long packageId, PackageLinesRequest request, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<bool>> MarkInvoicedAsync(long packageId, InvoicePackageRequest request, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<bool>> ReportUnsuppliedAsync(long packageId, UnsuppliedPackageRequest request, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<bool>> SendInvoiceLinkAsync(long packageId, SendInvoiceLinkCommand command, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<bool>> DeleteInvoiceLinkAsync(long packageId, long customerId, CancellationToken cancellationToken = default);
}
