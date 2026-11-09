using ECommerce.Application.Common;

namespace ECommerce.Application.Catalog;

public interface IBrandAdminService
{
    Task<IReadOnlyList<BrandDto>> ListAsync(string? search, CancellationToken cancellationToken = default);
    Task<Result<BrandDto>> CreateAsync(SaveBrandRequest request, CancellationToken cancellationToken = default);
    Task<Result<BrandDto>> UpdateAsync(int id, SaveBrandRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
