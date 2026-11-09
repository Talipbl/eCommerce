using ECommerce.Application.Common;

namespace ECommerce.Application.Catalog;

public interface ICategoryAdminService
{
    Task<IReadOnlyList<CategoryTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<Result<CategoryDto>> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<CategoryDto>> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result<CategoryDto>> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
