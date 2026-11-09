using ECommerce.Application.Common;

namespace ECommerce.Application.Marketplaces.Trendyol;

public interface ITrendyolListingService
{
    Task<IReadOnlyList<CategoryMappingDto>> ListCategoryMappingsAsync(CancellationToken cancellationToken = default);
    Task<Result<CategoryMappingDto>> MapCategoryAsync(int categoryId, MapCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result<CategoryMappingDto>> SetCategoryAttributesAsync(int categoryId, SetCategoryAttributesRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BrandMappingDto>> ListBrandMappingsAsync(CancellationToken cancellationToken = default);
    Task<Result<BrandMappingDto>> MapBrandAsync(int brandId, MapBrandRequest request, CancellationToken cancellationToken = default);
    /// <summary>Yayınları ürün adıyla listeler; son gönderilen önce gelir.</summary>
    Task<PagedResult<ListingSummaryDto>> ListAsync(ListingQuery query, CancellationToken cancellationToken = default);
    Task<Result<ListingDto>> GetAsync(int productId, CancellationToken cancellationToken = default);
    Task<Result<ListingDto>> PublishAsync(int productId, PublishListingRequest request, CancellationToken cancellationToken = default);
    Task<int> RefreshStatusesAsync(CancellationToken cancellationToken = default);
}
