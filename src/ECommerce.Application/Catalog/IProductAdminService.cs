using ECommerce.Application.Common;

namespace ECommerce.Application.Catalog;

public interface IProductAdminService
{
    Task<PagedResult<ProductListItemDto>> ListAsync(ProductAdminQuery query, CancellationToken cancellationToken = default);
    Task<Result<ProductDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ProductDetailDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<Result<ProductDetailDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<Result<ProductDetailDto>> AddVariantAsync(int id, VariantInput request, CancellationToken cancellationToken = default);
    Task<Result<ProductDetailDto>> UpdateVariantAsync(int id, int variantId, UpdateVariantRequest request, CancellationToken cancellationToken = default);
    Task<Result<ProductDetailDto>> RemoveVariantAsync(int id, int variantId, CancellationToken cancellationToken = default);
    Task<Result<ProductDetailDto>> PublishAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ProductDetailDto>> UnpublishAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
