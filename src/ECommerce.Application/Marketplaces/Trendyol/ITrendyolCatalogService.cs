namespace ECommerce.Application.Marketplaces.Trendyol;

public interface ITrendyolCatalogService
{
    Task<MarketplaceResult<IReadOnlyList<CategoryNodeDto>>> GetCategoryTreeAsync(CancellationToken cancellationToken = default);
    Task<MarketplaceResult<IReadOnlyList<CategorySearchResultDto>>> SearchCategoriesAsync(string term, int limit = 50, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<IReadOnlyList<CategoryAttributeDto>>> GetCategoryAttributesAsync(long categoryId, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<IReadOnlyList<AttributeValueDto>>> GetAttributeValuesAsync(long categoryId, long attributeId, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<IReadOnlyList<TrendyolBrandDto>>> SearchBrandsAsync(string name, CancellationToken cancellationToken = default);
}
