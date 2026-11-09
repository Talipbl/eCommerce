using Microsoft.Extensions.Options;
using Trendyol.Client.Http;

namespace Trendyol.Client.Products;

public interface ITrendyolProductClient
{
    Task<TrendyolResponse<ApprovedProductPage>> GetApprovedProductsAsync(ApprovedProductQuery query, CancellationToken cancellationToken = default);
    Task<TrendyolResponse<UnapprovedProductPage>> GetUnapprovedProductsAsync(UnapprovedProductQuery query, CancellationToken cancellationToken = default);
    Task<TrendyolResponse<BatchRequestResponse>> CreateProductsAsync(IReadOnlyList<CreateProductItem> items, CancellationToken cancellationToken = default);
    Task<TrendyolResponse<BatchRequestResponse>> UpdateApprovedContentAsync(IReadOnlyList<ApprovedContentUpdateItem> items, CancellationToken cancellationToken = default);
    Task<TrendyolResponse<BatchRequestResponse>> UpdateUnapprovedProductsAsync(IReadOnlyList<UnapprovedProductUpdateItem> items, CancellationToken cancellationToken = default);
    Task<TrendyolResponse<BatchRequestResponse>> UpdatePriceAndInventoryAsync(IReadOnlyList<PriceInventoryItem> items, CancellationToken cancellationToken = default);
    Task<TrendyolResponse<BatchRequestResponse>> DeleteProductsAsync(IReadOnlyList<string> barcodes, CancellationToken cancellationToken = default);
    Task<TrendyolResponse<BatchRequestResult>> GetBatchRequestResultAsync(string batchRequestId, CancellationToken cancellationToken = default);
}

public sealed class TrendyolProductClient(HttpClient httpClient, IOptions<TrendyolOptions> options) : ITrendyolProductClient
{
    private readonly TrendyolHttp _http = new(httpClient);
    private readonly string _products = $"/integration/product/sellers/{options.Value.SellerId}/products";
    private readonly string _inventory = $"/integration/inventory/sellers/{options.Value.SellerId}/products";
    private readonly string _createV2 = $"/integration/product/sellers/{options.Value.SellerId}/v2/products";

    public Task<TrendyolResponse<ApprovedProductPage>> GetApprovedProductsAsync(ApprovedProductQuery query, CancellationToken cancellationToken = default) =>
        _http.GetAsync<ApprovedProductPage>(new QueryString()
            .Add("barcode", query.Barcode)
            .AddCsv("barcodes", query.Barcodes)
            .Add("contentId", query.ContentId)
            .Add("stockCode", query.StockCode)
            .Add("productMainId", query.ProductMainId)
            .Add("brandIds", query.BrandIds)
            .Add("status", query.Status)
            .Add("startDate", query.StartDate)
            .Add("endDate", query.EndDate)
            .Add("dateQueryType", query.DateQueryType)
            .Add("page", query.Page)
            .Add("size", query.Size)
            .Add("nextPageToken", query.NextPageToken)
            .Apply($"{_products}/approved"), cancellationToken);

    public Task<TrendyolResponse<UnapprovedProductPage>> GetUnapprovedProductsAsync(UnapprovedProductQuery query, CancellationToken cancellationToken = default) =>
        _http.GetAsync<UnapprovedProductPage>(new QueryString()
            .Add("barcode", query.Barcode)
            .AddCsv("barcodes", query.Barcodes)
            .Add("stockCode", query.StockCode)
            .Add("productMainId", query.ProductMainId)
            .Add("brandIds", query.BrandIds)
            .Add("status", query.Status)
            .Add("startDate", query.StartDate)
            .Add("endDate", query.EndDate)
            .Add("dateQueryType", query.DateQueryType)
            .Add("page", query.Page)
            .Add("size", query.Size)
            .Add("nextPageToken", query.NextPageToken)
            .Apply($"{_products}/unapproved"), cancellationToken);

    public Task<TrendyolResponse<BatchRequestResponse>> CreateProductsAsync(IReadOnlyList<CreateProductItem> items, CancellationToken cancellationToken = default) =>
        _http.SendAsync<BatchRequestResponse>(HttpMethod.Post, _createV2, new ItemsRequest<CreateProductItem>(items), cancellationToken);

    public Task<TrendyolResponse<BatchRequestResponse>> UpdateApprovedContentAsync(IReadOnlyList<ApprovedContentUpdateItem> items, CancellationToken cancellationToken = default) =>
        _http.SendAsync<BatchRequestResponse>(HttpMethod.Post, $"{_products}/content-bulk-update", new ItemsRequest<ApprovedContentUpdateItem>(items), cancellationToken);

    public Task<TrendyolResponse<BatchRequestResponse>> UpdateUnapprovedProductsAsync(IReadOnlyList<UnapprovedProductUpdateItem> items, CancellationToken cancellationToken = default) =>
        _http.SendAsync<BatchRequestResponse>(HttpMethod.Post, $"{_products}/unapproved-bulk-update", new ItemsRequest<UnapprovedProductUpdateItem>(items), cancellationToken);

    public Task<TrendyolResponse<BatchRequestResponse>> UpdatePriceAndInventoryAsync(IReadOnlyList<PriceInventoryItem> items, CancellationToken cancellationToken = default) =>
        _http.SendAsync<BatchRequestResponse>(HttpMethod.Post, $"{_inventory}/price-and-inventory", new ItemsRequest<PriceInventoryItem>(items), cancellationToken);

    public Task<TrendyolResponse<BatchRequestResponse>> DeleteProductsAsync(IReadOnlyList<string> barcodes, CancellationToken cancellationToken = default) =>
        _http.SendAsync<BatchRequestResponse>(HttpMethod.Delete, _products, new ItemsRequest<DeleteProductItem>(barcodes.Select(b => new DeleteProductItem(b)).ToList()), cancellationToken);

    public Task<TrendyolResponse<BatchRequestResult>> GetBatchRequestResultAsync(string batchRequestId, CancellationToken cancellationToken = default) =>
        _http.GetAsync<BatchRequestResult>($"{_products}/batch-requests/{Uri.EscapeDataString(batchRequestId)}", cancellationToken);
}
