using Trendyol.Client.Orders;

namespace ECommerce.Application.Marketplaces.Trendyol;

public class TrendyolOrderService(ITrendyolOrderClient client, TimeProvider clock) : ITrendyolOrderService
{
    public const int MaxPageSize = 200;
    public const int MaxAccessibleRecords = 10000;
    public static readonly TimeSpan MaxDateRange = TimeSpan.FromDays(14);

    public async Task<MarketplaceResult<ShipmentPackagePage>> GetPackagesAsync(OrderPackagesQuery query, CancellationToken cancellationToken = default)
    {
        var error = Validate(query);
        if (error is not null)
            return MarketplaceResult<ShipmentPackagePage>.Failure(MarketplaceError.Validation(error));

        var response = await client.GetShipmentPackagesAsync(new ShipmentPackageQuery
        {
            StartDate = query.StartDate,
            EndDate = query.EndDate,
            Status = query.Status,
            OrderNumber = query.OrderNumber,
            ShipmentPackageId = query.ShipmentPackageId,
            OrderByDirection = query.OrderByDirection,
            Page = query.Page,
            Size = query.Size
        }, cancellationToken);

        return response.ToMarketplaceResult();
    }

    public async Task<MarketplaceResult<bool>> StartPickingAsync(long packageId, PackageLinesRequest request, CancellationToken cancellationToken = default) =>
        (await client.UpdatePackageStatusAsync(packageId,
            new UpdatePackageStatusRequest(request.Lines, ShipmentPackageStatuses.Picking, new Dictionary<string, string>()), cancellationToken))
        .ToMarketplaceResult(_ => true);

    public async Task<MarketplaceResult<bool>> MarkInvoicedAsync(long packageId, InvoicePackageRequest request, CancellationToken cancellationToken = default) =>
        (await client.UpdatePackageStatusAsync(packageId,
            new UpdatePackageStatusRequest(request.Lines, ShipmentPackageStatuses.Invoiced, new Dictionary<string, string> { ["invoiceNumber"] = request.InvoiceNumber }),
            cancellationToken))
        .ToMarketplaceResult(_ => true);

    public async Task<MarketplaceResult<bool>> ReportUnsuppliedAsync(long packageId, UnsuppliedPackageRequest request, CancellationToken cancellationToken = default) =>
        (await client.ReportUnsuppliedAsync(packageId, new UnsuppliedRequest(request.Lines, (int)request.Reason), cancellationToken))
        .ToMarketplaceResult(_ => true);

    public async Task<MarketplaceResult<bool>> SendInvoiceLinkAsync(long packageId, SendInvoiceLinkCommand command, CancellationToken cancellationToken = default) =>
        (await client.SendInvoiceLinkAsync(new SendInvoiceLinkRequest(
            command.InvoiceLink, packageId, command.InvoiceDate?.ToUnixTimeMilliseconds(), command.InvoiceNumber), cancellationToken))
        .ToMarketplaceResult(_ => true);

    public async Task<MarketplaceResult<bool>> DeleteInvoiceLinkAsync(long packageId, long customerId, CancellationToken cancellationToken = default) =>
        (await client.DeleteInvoiceLinkAsync(new DeleteInvoiceLinkRequest(packageId, customerId), cancellationToken))
        .ToMarketplaceResult(_ => true);

    private string? Validate(OrderPackagesQuery query)
    {
        if (query.Size is < 1 or > MaxPageSize)
            return $"Sayfa boyutu 1-{MaxPageSize} arasında olmalıdır.";
        if (query.Page < 0 || (query.Page + 1) * query.Size > MaxAccessibleRecords)
            return $"Trendyol en fazla {MaxAccessibleRecords} kayda sayfalı erişim veriyor; filtreyi daraltın.";
        if (query.Status is not null && !ShipmentPackageStatuses.All.Contains(query.Status))
            return $"Geçersiz paket statüsü: {query.Status}.";
        if (query.StartDate is { } start && query.EndDate is { } end)
        {
            if (end < start)
                return "Bitiş tarihi başlangıç tarihinden önce olamaz.";
            if (end - start > MaxDateRange)
                return "Tarih aralığı en fazla 14 gün olabilir.";
        }
        if (query.StartDate < clock.GetUtcNow().AddMonths(-1))
            return "Trendyol en fazla son bir ayın siparişlerini döndürür.";

        return null;
    }
}
