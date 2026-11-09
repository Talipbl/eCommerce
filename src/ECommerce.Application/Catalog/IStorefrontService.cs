using ECommerce.Application.Common;

namespace ECommerce.Application.Catalog;

public interface IStorefrontService
{
    Task<IReadOnlyList<StoreCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<StoreProductCard>> SearchProductsAsync(StoreProductQuery query, CancellationToken cancellationToken = default);
    Task<Result<StoreProductDetail>> GetProductAsync(string slug, CancellationToken cancellationToken = default);
}
