using ECommerce.Application.Common;
using ECommerce.Catalog;
using ECommerce.Core.Persistence;
using ECommerce.Core.Text;

namespace ECommerce.Application.Catalog;

public class ProductAdminService(
    IRepository<Product> products,
    IRepository<ProductVariant> variants,
    IRepository<Brand> brands,
    IRepository<Category> categories,
    IUnitOfWork unitOfWork,
    IMediaUrlBuilder media) : IProductAdminService
{
    public async Task<PagedResult<ProductListItemDto>> ListAsync(ProductAdminQuery query, CancellationToken cancellationToken = default)
    {
        var size = Math.Clamp(query.Size, 1, 100);
        var page = Math.Max(query.Page, 0);
        var term = query.Search?.Trim().ToLower();

        IQueryable<Product> Filter(IQueryable<Product> q) => q
            .Where(p => term == null || p.Name.ToLower().Contains(term) || p.ProductCode.ToLower().Contains(term))
            .Where(p => query.CategoryId == null || p.CategoryId == query.CategoryId)
            .Where(p => query.BrandId == null || p.BrandId == query.BrandId)
            .Where(p => query.IsActive == null || p.IsActive == query.IsActive);

        var total = await products.CountAsync(q => Filter(q), cancellationToken);
        var rows = await products.ListAsync(q => Filter(q)
            .OrderByDescending(p => p.CreatedDate).ThenByDescending(p => p.Id)
            .Skip(page * size).Take(size)
            .Select(p => new
            {
                p.Id, p.Name, p.Slug, p.ProductCode, p.BrandId, p.CategoryId, p.IsActive,
                VariantCount = p.Variants.Count,
                TotalStock = p.Variants.Sum(v => v.StockQuantity),
                MinPrice = p.Variants.Min(v => (decimal?)v.Price),
                ImagePath = p.Images.OrderBy(i => i.SortOrder).Select(i => i.Path).FirstOrDefault()
            }), cancellationToken);

        return new PagedResult<ProductListItemDto>(page, size, total, rows
            .Select(r => new ProductListItemDto(r.Id, r.Name, r.Slug, r.ProductCode, r.BrandId, r.CategoryId, r.IsActive,
                r.VariantCount, r.TotalStock, r.MinPrice, r.ImagePath is null ? null : media.ToPublicUrl(r.ImagePath)))
            .ToList());
    }

    public async Task<Result<ProductDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await FindAsync(id, cancellationToken);
        return product is null ? NotFound() : Result<ProductDetailDto>.Success(ToDetail(product));
    }

    public async Task<Result<ProductDetailDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var slug = request.Slug ?? Slug.From(request.Name);
        var error = await ValidateReferencesAsync(null, slug, request.BrandId, request.CategoryId, cancellationToken)
            ?? await ValidateVariantUniquenessAsync(request.Variants, cancellationToken);
        if (error is not null)
            return error;

        var product = Product.Create(request.Name, slug, request.ProductCode, request.BrandId, request.CategoryId, request.VatRate, request.Description);
        foreach (var v in request.Variants)
            product.AddVariant(v.Sku, v.Barcode, v.Price, v.ListPrice, v.StockQuantity, ToAttributes(v.Attributes));

        products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ProductDetailDto>.Success(ToDetail(product));
    }

    public Task<Result<ProductDetailDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default) =>
        ModifyAsync(id, async product =>
            await ValidateReferencesAsync(id, request.Slug, request.BrandId, request.CategoryId, cancellationToken)
            ?? Apply(() => product.UpdateDetails(request.Name, request.Slug, request.ProductCode, request.BrandId, request.CategoryId, request.VatRate, request.Description)),
            cancellationToken);

    public Task<Result<ProductDetailDto>> AddVariantAsync(int id, VariantInput request, CancellationToken cancellationToken = default) =>
        ModifyAsync(id, async product =>
            await ValidateVariantUniquenessAsync([request], cancellationToken)
            ?? Apply(() => product.AddVariant(request.Sku, request.Barcode, request.Price, request.ListPrice, request.StockQuantity, ToAttributes(request.Attributes))),
            cancellationToken);

    public Task<Result<ProductDetailDto>> UpdateVariantAsync(int id, int variantId, UpdateVariantRequest request, CancellationToken cancellationToken = default) =>
        ModifyAsync(id, product =>
        {
            var variant = product.Variants.FirstOrDefault(v => v.Id == variantId);
            if (variant is null)
                return Task.FromResult<AppError?>(Errors.NotFound("Varyant bulunamadı."));

            variant.ChangePrice(request.Price, request.ListPrice);
            variant.SetStock(request.StockQuantity);
            variant.SetAttributes(ToAttributes(request.Attributes));
            product.SetVariantActive(variant, request.IsActive);
            return Task.FromResult<AppError?>(null);
        }, cancellationToken);

    public Task<Result<ProductDetailDto>> RemoveVariantAsync(int id, int variantId, CancellationToken cancellationToken = default) =>
        ModifyAsync(id, product =>
        {
            var variant = product.Variants.FirstOrDefault(v => v.Id == variantId);
            if (variant is null)
                return Task.FromResult<AppError?>(Errors.NotFound("Varyant bulunamadı."));

            product.RemoveVariant(variant);
            return Task.FromResult<AppError?>(null);
        }, cancellationToken);

    public Task<Result<ProductDetailDto>> PublishAsync(int id, CancellationToken cancellationToken = default) =>
        ModifyAsync(id, product => Task.FromResult(Apply(product.Publish)), cancellationToken);

    public Task<Result<ProductDetailDto>> UnpublishAsync(int id, CancellationToken cancellationToken = default) =>
        ModifyAsync(id, product => Task.FromResult(Apply(product.Unpublish)), cancellationToken);

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await FindAsync(id, cancellationToken);
        if (product is null)
            return Result<bool>.NotFound("Ürün bulunamadı.");

        products.Remove(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    internal Task<Product?> FindAsync(int id, CancellationToken cancellationToken) =>
        products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    private async Task<Result<ProductDetailDto>> ModifyAsync(int id, Func<Product, Task<AppError?>> change, CancellationToken cancellationToken)
    {
        var product = await FindAsync(id, cancellationToken);
        if (product is null)
            return NotFound();

        var error = await change(product);
        if (error is not null)
            return error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ProductDetailDto>.Success(ToDetail(product));
    }

    private async Task<AppError?> ValidateReferencesAsync(int? id, string slug, int brandId, int categoryId, CancellationToken cancellationToken)
    {
        if (await products.AnyAsync(p => p.Slug == slug && p.Id != id, cancellationToken))
            return Errors.Conflict($"'{slug}' adresi başka bir üründe kullanılıyor.");
        if (!await brands.AnyAsync(b => b.Id == brandId, cancellationToken))
            return Errors.Invalid("Marka bulunamadı.");
        if (!await categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
            return Errors.Invalid("Kategori bulunamadı.");
        return null;
    }

    private async Task<AppError?> ValidateVariantUniquenessAsync(IReadOnlyList<VariantInput> inputs, CancellationToken cancellationToken)
    {
        var skus = inputs.Select(v => v.Sku.Trim()).ToList();
        var barcodes = inputs.Select(v => v.Barcode.Trim()).ToList();
        var existing = await variants.ListAsync(q => q
            .Where(v => skus.Contains(v.Sku) || barcodes.Contains(v.Barcode))
            .Select(v => v.Barcode), cancellationToken);

        return existing.Count > 0
            ? Errors.Conflict($"Stok kodu veya barkod başka bir üründe kullanılıyor: {string.Join(", ", existing)}.")
            : null;
    }

    private static AppError? Apply(Action action)
    {
        action();
        return null;
    }

    private static List<VariantAttribute> ToAttributes(IEnumerable<VariantAttributeDto> attributes) =>
        attributes.Select(a => new VariantAttribute(a.Name.Trim(), a.Value.Trim())).ToList();

    private static Result<ProductDetailDto> NotFound() => Result<ProductDetailDto>.NotFound("Ürün bulunamadı.");

    internal ProductDetailDto ToDetail(Product p) => new(
        p.Id, p.Name, p.Slug, p.ProductCode, p.BrandId, p.CategoryId, p.VatRate, p.Description, p.IsActive,
        p.Variants.Select(v => new VariantDto(v.Id, v.Sku, v.Barcode, v.Price, v.ListPrice, v.StockQuantity, v.IsActive,
            v.Attributes.Select(a => new VariantAttributeDto(a.Name, a.Value)).ToList())).ToList(),
        p.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto(i.Id, media.ToPublicUrl(i.Path), i.AltText, i.SortOrder)).ToList());
}
