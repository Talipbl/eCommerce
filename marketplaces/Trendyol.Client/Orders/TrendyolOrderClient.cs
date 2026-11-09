using Microsoft.Extensions.Options;
using Trendyol.Client.Http;

namespace Trendyol.Client.Orders;

public interface ITrendyolOrderClient
{
    Task<TrendyolResponse<ShipmentPackagePage>> GetShipmentPackagesAsync(ShipmentPackageQuery query, CancellationToken cancellationToken = default);
    Task<TrendyolResponse<TrendyolEmpty>> UpdatePackageStatusAsync(long packageId, UpdatePackageStatusRequest request, CancellationToken cancellationToken = default);
    Task<TrendyolResponse<TrendyolEmpty>> ReportUnsuppliedAsync(long packageId, UnsuppliedRequest request, CancellationToken cancellationToken = default);
    Task<TrendyolResponse<TrendyolEmpty>> SendInvoiceLinkAsync(SendInvoiceLinkRequest request, CancellationToken cancellationToken = default);
    Task<TrendyolResponse<TrendyolEmpty>> DeleteInvoiceLinkAsync(DeleteInvoiceLinkRequest request, CancellationToken cancellationToken = default);
}

public sealed class TrendyolOrderClient(HttpClient httpClient, IOptions<TrendyolOptions> options) : ITrendyolOrderClient
{
    private readonly TrendyolHttp _http = new(httpClient);
    private readonly string _orders = $"/integration/order/sellers/{options.Value.SellerId}";
    private readonly string _invoices = $"/integration/sellers/{options.Value.SellerId}/seller-invoice-links";

    public Task<TrendyolResponse<ShipmentPackagePage>> GetShipmentPackagesAsync(ShipmentPackageQuery query, CancellationToken cancellationToken = default) =>
        _http.GetAsync<ShipmentPackagePage>(new QueryString()
            .Add("startDate", query.StartDate)
            .Add("endDate", query.EndDate)
            .Add("status", query.Status)
            .Add("orderNumber", query.OrderNumber)
            .Add("shipmentPackageIds", query.ShipmentPackageId)
            .Add("orderByField", query.OrderByDirection is null ? null : "PackageLastModifiedDate")
            .Add("orderByDirection", query.OrderByDirection)
            .Add("page", query.Page)
            .Add("size", query.Size)
            .Apply($"{_orders}/v2/orders"), cancellationToken);

    public Task<TrendyolResponse<TrendyolEmpty>> UpdatePackageStatusAsync(long packageId, UpdatePackageStatusRequest request, CancellationToken cancellationToken = default) =>
        _http.SendAsync<TrendyolEmpty>(HttpMethod.Put, $"{_orders}/shipment-packages/{packageId}", request, cancellationToken);

    public Task<TrendyolResponse<TrendyolEmpty>> ReportUnsuppliedAsync(long packageId, UnsuppliedRequest request, CancellationToken cancellationToken = default) =>
        _http.SendAsync<TrendyolEmpty>(HttpMethod.Put, $"{_orders}/shipment-packages/{packageId}/items/unsupplied", request, cancellationToken);

    public Task<TrendyolResponse<TrendyolEmpty>> SendInvoiceLinkAsync(SendInvoiceLinkRequest request, CancellationToken cancellationToken = default) =>
        _http.SendAsync<TrendyolEmpty>(HttpMethod.Post, _invoices, request, cancellationToken);

    public Task<TrendyolResponse<TrendyolEmpty>> DeleteInvoiceLinkAsync(DeleteInvoiceLinkRequest request, CancellationToken cancellationToken = default) =>
        _http.SendAsync<TrendyolEmpty>(HttpMethod.Post, $"{_invoices}/delete", request, cancellationToken);
}
