using Trendyol.Client.Products;

namespace ECommerce.Application.Marketplaces.Trendyol;

public interface ITrendyolProductService
{
    Task<MarketplaceResult<ApprovedProductPage>> GetApprovedAsync(ApprovedProductQuery query, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<UnapprovedProductPage>> GetUnapprovedAsync(UnapprovedProductQuery query, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<BatchAcceptedDto>> CreateAsync(CreateTrendyolProductsRequest request, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<BatchAcceptedDto>> UpdateContentAsync(UpdateTrendyolContentRequest request, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<BatchAcceptedDto>> UpdateUnapprovedAsync(UpdateUnapprovedTrendyolProductsRequest request, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<BatchAcceptedDto>> UpdatePriceAndInventoryAsync(UpdatePriceInventoryRequest request, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<BatchAcceptedDto>> DeleteAsync(DeleteTrendyolProductsRequest request, CancellationToken cancellationToken = default);
}
