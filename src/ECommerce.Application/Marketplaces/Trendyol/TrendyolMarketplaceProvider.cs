using ECommerce.Application.Common;
using ECommerce.Marketplaces;
using ECommerce.Ordering;
using Trendyol.Client.Orders;
using Trendyol.Client.Products;

namespace ECommerce.Application.Marketplaces.Trendyol;

/// <summary>
/// <see cref="IMarketplaceProvider"/>'ın Trendyol implementasyonu: mevcut Trendyol servislerini ortak modele çevirir.
/// </summary>
public class TrendyolMarketplaceProvider(
    ITrendyolListingService listings,
    ITrendyolProductService products,
    ITrendyolOrderService orders) : IMarketplaceProvider
{
    private const int MaxStock = 20000;

    public string Name => MarketplaceNames.Trendyol;

    public OrderChannel Channel => OrderChannel.Trendyol;

    public string OrderNumberPrefix => "TY";

    public TimeSpan MaxOrderWindow => TrendyolOrderService.MaxDateRange;

    // Trendyol en fazla son bir ayı döndürür; ay uzunluğu farkı için 28 gün kullanılır.
    public TimeSpan MaxOrderLookback => TimeSpan.FromDays(28);

    public int MaxOrderPages => TrendyolOrderService.MaxAccessibleRecords / TrendyolOrderService.MaxPageSize;

    public int MaxPriceInventoryItems => 1000;

    public Task<Result<ListingDto>> PublishProductAsync(int productId, PublishListingRequest request, CancellationToken cancellationToken = default) =>
        listings.PublishAsync(productId, request, cancellationToken);

    public async Task<MarketplaceResult<bool>> PushPriceInventoryAsync(IReadOnlyList<MarketplacePriceStock> items, CancellationToken cancellationToken = default)
    {
        var request = new UpdatePriceInventoryRequest(items
            .Select(i => new PriceInventoryItem(i.Barcode, Math.Clamp(i.Quantity, 0, MaxStock), i.SalePrice, Math.Max(i.ListPrice, i.SalePrice)))
            .ToList());
        var result = await products.UpdatePriceAndInventoryAsync(request, cancellationToken);
        return result.Succeeded ? MarketplaceResult<bool>.Success(true) : MarketplaceResult<bool>.Failure(result.Error!);
    }

    public async Task<MarketplaceResult<MarketplaceOrderPage>> FetchOrdersAsync(DateTimeOffset from, DateTimeOffset to, int page, CancellationToken cancellationToken = default)
    {
        var result = await orders.GetPackagesAsync(
            new OrderPackagesQuery(from, to, null, null, null, page, TrendyolOrderService.MaxPageSize, "ASC"), cancellationToken);
        if (!result.Succeeded)
            return MarketplaceResult<MarketplaceOrderPage>.Failure(result.Error!);

        var packages = result.Value!.Content ?? [];
        return MarketplaceResult<MarketplaceOrderPage>.Success(new MarketplaceOrderPage(packages.Select(ToOrder).ToList(), result.Value.TotalPages));
    }

    public async Task<MarketplaceResult<bool>> UpdateOrderStatusAsync(MarketplaceStatusCommand command, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(command.ExternalReference, out var packageId))
            return MarketplaceResult<bool>.Failure(MarketplaceError.Validation($"Geçersiz Trendyol paket numarası: {command.ExternalReference}."));

        var lines = command.Lines.Select(l => new PackageLine(l.LineId, l.Quantity)).ToList();
        return command.Action switch
        {
            MarketplaceOrderAction.Picking => await orders.StartPickingAsync(packageId, new PackageLinesRequest(lines), cancellationToken),
            MarketplaceOrderAction.Invoiced when string.IsNullOrWhiteSpace(command.InvoiceNumber) =>
                MarketplaceResult<bool>.Failure(MarketplaceError.Validation("Invoiced bildirimi için fatura numarası gerekir.")),
            MarketplaceOrderAction.Invoiced => await orders.MarkInvoicedAsync(packageId, new InvoicePackageRequest(lines, command.InvoiceNumber!), cancellationToken),
            _ => MarketplaceResult<bool>.Failure(MarketplaceError.Validation($"Desteklenmeyen işlem: {command.Action}."))
        };
    }

    private static MarketplaceOrder ToOrder(ShipmentPackage package)
    {
        var address = package.ShipmentAddress ?? new TrendyolAddress();
        var customerName = $"{package.CustomerFirstName} {package.CustomerLastName}".Trim();
        if (customerName.Length == 0)
            customerName = "Trendyol müşterisi";

        var lines = (package.Lines ?? []).Select(l =>
        {
            var net = l.LineGrossAmount - l.LineTotalDiscount;
            var unitPrice = l.Quantity > 0 && net > 0 ? Math.Round(net / l.Quantity, 2) : l.LineUnitPrice;
            return new MarketplaceOrderLine(l.LineId, l.Barcode, l.StockCode, l.ProductName ?? l.Barcode ?? "Ürün", l.Quantity, unitPrice, (int)(l.VatRate ?? 0));
        }).ToList();

        return new MarketplaceOrder(
            package.ShipmentPackageId.ToString(),
            MapStatus(package.Status),
            package.CustomerEmail ?? "-",
            customerName,
            new OrderAddress(address.FullName ?? customerName, address.Phone ?? "-", address.City ?? "-", address.District ?? "-", address.FullAddress ?? "-", address.PostalCode),
            lines,
            DateTimeOffset.FromUnixTimeMilliseconds(package.OrderDate).UtcDateTime,
            package.CargoProviderName,
            package.CargoTrackingNumber?.ToString());
    }

    public static OrderStatus? MapStatus(string? status) => status switch
    {
        ShipmentPackageStatuses.Created or ShipmentPackageStatuses.Awaiting or ShipmentPackageStatuses.Verified => OrderStatus.Confirmed,
        ShipmentPackageStatuses.Picking or ShipmentPackageStatuses.Invoiced => OrderStatus.Preparing,
        ShipmentPackageStatuses.Shipped or ShipmentPackageStatuses.AtCollectionPoint => OrderStatus.Shipped,
        ShipmentPackageStatuses.Delivered => OrderStatus.Delivered,
        ShipmentPackageStatuses.Cancelled or ShipmentPackageStatuses.UnSupplied => OrderStatus.Cancelled,
        _ => null
    };
}
