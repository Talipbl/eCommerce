using ECommerce.Marketplaces;
using Trendyol.Client.Products;

namespace ECommerce.Application.Marketplaces.Trendyol;

public class TrendyolProductService(ITrendyolProductClient client, TrendyolBatchTracker tracker) : ITrendyolProductService
{
    public async Task<MarketplaceResult<ApprovedProductPage>> GetApprovedAsync(ApprovedProductQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Size is < 1 or > 100)
            return MarketplaceResult<ApprovedProductPage>.Failure(MarketplaceError.Validation("Sayfa boyutu 1-100 arasında olmalıdır."));

        return (await client.GetApprovedProductsAsync(query, cancellationToken)).ToMarketplaceResult();
    }

    public async Task<MarketplaceResult<UnapprovedProductPage>> GetUnapprovedAsync(UnapprovedProductQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Size is < 1 or > 1000)
            return MarketplaceResult<UnapprovedProductPage>.Failure(MarketplaceError.Validation("Sayfa boyutu 1-1000 arasında olmalıdır."));

        return (await client.GetUnapprovedProductsAsync(query, cancellationToken)).ToMarketplaceResult();
    }

    public async Task<MarketplaceResult<BatchAcceptedDto>> CreateAsync(CreateTrendyolProductsRequest request, CancellationToken cancellationToken = default) =>
        await tracker.TrackAsync(await client.CreateProductsAsync(request.Items, cancellationToken),
            MarketplaceBatchOperation.ProductCreate, request.Items.Count, cancellationToken);

    public async Task<MarketplaceResult<BatchAcceptedDto>> UpdateContentAsync(UpdateTrendyolContentRequest request, CancellationToken cancellationToken = default) =>
        await tracker.TrackAsync(await client.UpdateApprovedContentAsync(request.Items, cancellationToken),
            MarketplaceBatchOperation.ProductContentUpdate, request.Items.Count, cancellationToken);

    public async Task<MarketplaceResult<BatchAcceptedDto>> UpdateUnapprovedAsync(UpdateUnapprovedTrendyolProductsRequest request, CancellationToken cancellationToken = default) =>
        await tracker.TrackAsync(await client.UpdateUnapprovedProductsAsync(request.Items, cancellationToken),
            MarketplaceBatchOperation.UnapprovedProductUpdate, request.Items.Count, cancellationToken);

    public async Task<MarketplaceResult<BatchAcceptedDto>> UpdatePriceAndInventoryAsync(UpdatePriceInventoryRequest request, CancellationToken cancellationToken = default) =>
        await tracker.TrackAsync(await client.UpdatePriceAndInventoryAsync(request.Items, cancellationToken),
            MarketplaceBatchOperation.PriceInventory, request.Items.Count, cancellationToken);

    public async Task<MarketplaceResult<BatchAcceptedDto>> DeleteAsync(DeleteTrendyolProductsRequest request, CancellationToken cancellationToken = default) =>
        await tracker.TrackAsync(await client.DeleteProductsAsync(request.Barcodes, cancellationToken),
            MarketplaceBatchOperation.ProductDelete, request.Barcodes.Count, cancellationToken);
}
