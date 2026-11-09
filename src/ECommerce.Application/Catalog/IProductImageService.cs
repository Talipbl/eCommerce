using ECommerce.Application.Common;

namespace ECommerce.Application.Catalog;

public interface IProductImageService
{
    Task<Result<ProductDetailDto>> UploadAsync(int productId, Stream content, string contentType, long length, string? altText, CancellationToken cancellationToken = default);
    Task<Result<ProductDetailDto>> DeleteAsync(int productId, int imageId, CancellationToken cancellationToken = default);
    Task<Result<ProductDetailDto>> ReorderAsync(int productId, IReadOnlyList<int> imageIds, CancellationToken cancellationToken = default);
}
