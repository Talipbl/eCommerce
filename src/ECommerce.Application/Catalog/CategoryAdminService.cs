using ECommerce.Application.Common;
using ECommerce.Catalog;
using ECommerce.Core.Persistence;
using ECommerce.Core.Text;
using MapsterMapper;

namespace ECommerce.Application.Catalog;

public class CategoryAdminService(IRepository<Category> categories, IRepository<Product> products, IUnitOfWork unitOfWork, IMapper mapper)
    : ICategoryAdminService
{
    public async Task<IReadOnlyList<CategoryTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var all = await categories.ListAsync(cancellationToken: cancellationToken);
        return BuildTree(all, null);
    }

    public async Task<Result<CategoryDto>> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var slug = request.Slug ?? Slug.From(request.Name);
        var error = await ValidateAsync(null, slug, request.ParentId, cancellationToken);
        if (error is not null)
            return error;

        var category = Category.Create(request.Name, slug, request.ParentId, request.Description, request.SortOrder);
        category.SetActive(request.IsActive);
        categories.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CategoryDto>.Success(mapper.Map<CategoryDto>(category));
    }

    public async Task<Result<CategoryDto>> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
            return Result<CategoryDto>.NotFound("Kategori bulunamadı.");

        var slug = request.Slug ?? Slug.From(request.Name);
        var error = await ValidateAsync(id, slug, request.ParentId, cancellationToken);
        if (error is not null)
            return error;

        category.Update(request.Name, slug, request.Description, request.SortOrder);
        category.MoveTo(request.ParentId);
        category.SetActive(request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CategoryDto>.Success(mapper.Map<CategoryDto>(category));
    }

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
            return Result<bool>.NotFound("Kategori bulunamadı.");
        if (await categories.AnyAsync(c => c.ParentId == id, cancellationToken))
            return Result<bool>.Conflict("Alt kategorisi olan kategori silinemez.");
        if (await products.AnyAsync(p => p.CategoryId == id, cancellationToken))
            return Result<bool>.Conflict("Ürünü olan kategori silinemez.");

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private async Task<AppError?> ValidateAsync(int? id, string slug, int? parentId, CancellationToken cancellationToken)
    {
        if (await categories.AnyAsync(c => c.Slug == slug && c.Id != id, cancellationToken))
            return Errors.Conflict($"'{slug}' adresi başka bir kategoride kullanılıyor.");

        if (parentId is null)
            return null;

        var all = await categories.ListAsync(cancellationToken: cancellationToken);
        var byId = all.ToDictionary(c => c.Id);
        if (!byId.ContainsKey(parentId.Value))
            return Errors.Invalid("Üst kategori bulunamadı.");

        // Yeni üst kategoriden köke doğru çıkarken kendisine rastlanırsa döngü oluşur.
        for (int? current = parentId; current is not null; current = byId[current.Value].ParentId)
        {
            if (current == id)
                return Errors.Invalid("Kategori kendi alt kategorisinin altına taşınamaz.");
        }

        return null;
    }

    private static List<CategoryTreeDto> BuildTree(IReadOnlyList<Category> all, int? parentId) =>
        all.Where(c => c.ParentId == parentId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryTreeDto(c.Id, c.Name, c.Slug, c.ParentId, c.SortOrder, c.IsActive, BuildTree(all, c.Id)))
            .ToList();
}
