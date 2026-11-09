using ECommerce.Application.Common;
using ECommerce.Catalog;
using ECommerce.Core.Persistence;
using ECommerce.Core.Text;

namespace ECommerce.Application.Catalog;

public class BrandAdminService(IRepository<Brand> brands, IRepository<Product> products, IUnitOfWork unitOfWork) : IBrandAdminService
{
    public async Task<IReadOnlyList<BrandDto>> ListAsync(string? search, CancellationToken cancellationToken = default)
    {
        var term = search?.Trim().ToLower();
        return await brands.ListAsync(q => q
            .Where(b => term == null || b.Name.ToLower().Contains(term))
            .OrderBy(b => b.Name)
            .Select(b => new BrandDto(b.Id, b.Name, b.Slug, b.IsActive)), cancellationToken);
    }

    public async Task<Result<BrandDto>> CreateAsync(SaveBrandRequest request, CancellationToken cancellationToken = default)
    {
        var slug = request.Slug ?? Slug.From(request.Name);
        if (await brands.AnyAsync(b => b.Slug == slug, cancellationToken))
            return Result<BrandDto>.Conflict($"'{slug}' adresi başka bir markada kullanılıyor.");

        var brand = Brand.Create(request.Name, slug);
        brand.SetActive(request.IsActive);
        brands.Add(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BrandDto>.Success(ToDto(brand));
    }

    public async Task<Result<BrandDto>> UpdateAsync(int id, SaveBrandRequest request, CancellationToken cancellationToken = default)
    {
        var brand = await brands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (brand is null)
            return Result<BrandDto>.NotFound("Marka bulunamadı.");

        var slug = request.Slug ?? Slug.From(request.Name);
        if (await brands.AnyAsync(b => b.Slug == slug && b.Id != id, cancellationToken))
            return Result<BrandDto>.Conflict($"'{slug}' adresi başka bir markada kullanılıyor.");

        brand.Update(request.Name, slug);
        brand.SetActive(request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BrandDto>.Success(ToDto(brand));
    }

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var brand = await brands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (brand is null)
            return Result<bool>.NotFound("Marka bulunamadı.");
        if (await products.AnyAsync(p => p.BrandId == id, cancellationToken))
            return Result<bool>.Conflict("Ürünü olan marka silinemez.");

        brands.Remove(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private static BrandDto ToDto(Brand b) => new(b.Id, b.Name, b.Slug, b.IsActive);
}
