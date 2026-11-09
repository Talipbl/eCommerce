using ECommerce.Application.Common;
using ECommerce.Catalog;
using ECommerce.Core.Persistence;

namespace ECommerce.Application.Catalog;

/// <summary>
/// Vitrin sorguları: yalnızca yayındaki ürünler, aktif marka ve (üst kategorileriyle birlikte) aktif kategoriler.
/// </summary>
public class StorefrontService(
    IRepository<Product> products,
    IRepository<Category> categories,
    IRepository<Brand> brands,
    IMediaUrlBuilder media) : IStorefrontService
{
    public async Task<IReadOnlyList<StoreCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var visible = await VisibleCategoriesAsync(cancellationToken);
        return BuildTree(visible.Values.ToList(), null);
    }

    public async Task<PagedResult<StoreProductCard>> SearchProductsAsync(StoreProductQuery query, CancellationToken cancellationToken = default)
    {
        var size = Math.Clamp(query.Size, 1, 100);
        var page = Math.Max(query.Page, 0);
        var visibleCategories = await VisibleCategoriesAsync(cancellationToken);
        var activeBrands = (await brands.ListAsync(b => b.IsActive, cancellationToken)).ToDictionary(b => b.Id);

        var categoryIds = visibleCategories.Keys.ToList();
        if (query.CategorySlug is not null)
        {
            var root = visibleCategories.Values.FirstOrDefault(c => c.Slug == query.CategorySlug);
            if (root is null)
                return new PagedResult<StoreProductCard>(page, size, 0, []);
            categoryIds = Descendants(visibleCategories.Values.ToList(), root.Id).ToList();
        }

        var brandIds = activeBrands.Keys.ToList();
        if (query.BrandSlug is not null)
            brandIds = activeBrands.Values.Where(b => b.Slug == query.BrandSlug).Select(b => b.Id).ToList();

        var term = query.Search?.Trim().ToLower();

        IQueryable<CardRow> Cards(IQueryable<Product> q) => q
            .Where(p => p.IsActive && categoryIds.Contains(p.CategoryId) && brandIds.Contains(p.BrandId))
            .Where(p => term == null || p.Name.ToLower().Contains(term))
            .Where(p => p.Variants.Any(v => v.IsActive))
            .Select(p => new CardRow
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                BrandId = p.BrandId,
                CreatedDate = p.CreatedDate,
                MinPrice = p.Variants.Where(v => v.IsActive).Min(v => v.Price),
                ListPrice = p.Variants.Where(v => v.IsActive).OrderBy(v => v.Price).Select(v => v.ListPrice).FirstOrDefault(),
                InStock = p.Variants.Any(v => v.IsActive && v.StockQuantity > 0),
                ImagePath = p.Images.OrderBy(i => i.SortOrder).Select(i => i.Path).FirstOrDefault()
            })
            .Where(c => query.MinPrice == null || c.MinPrice >= query.MinPrice)
            .Where(c => query.MaxPrice == null || c.MinPrice <= query.MaxPrice)
            .Where(c => !query.InStockOnly || c.InStock);

        IQueryable<CardRow> Sorted(IQueryable<CardRow> q) => query.Sort switch
        {
            StoreProductSort.PriceAscending => q.OrderBy(c => c.MinPrice).ThenBy(c => c.Id),
            StoreProductSort.PriceDescending => q.OrderByDescending(c => c.MinPrice).ThenBy(c => c.Id),
            StoreProductSort.Name => q.OrderBy(c => c.Name).ThenBy(c => c.Id),
            _ => q.OrderByDescending(c => c.CreatedDate).ThenByDescending(c => c.Id)
        };

        var total = await products.CountAsync(q => Cards(q), cancellationToken);
        var rows = await products.ListAsync(q => Sorted(Cards(q)).Skip(page * size).Take(size), cancellationToken);

        return new PagedResult<StoreProductCard>(page, size, total, rows
            .Select(r => new StoreProductCard(r.Id, r.Name, r.Slug, activeBrands[r.BrandId].Name, r.MinPrice, r.ListPrice, r.InStock,
                r.ImagePath is null ? null : media.ToPublicUrl(r.ImagePath)))
            .ToList());
    }

    public async Task<Result<StoreProductDetail>> GetProductAsync(string slug, CancellationToken cancellationToken = default)
    {
        var product = await products.FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive, cancellationToken);
        var visibleCategories = await VisibleCategoriesAsync(cancellationToken);
        var brand = product is null ? null : await brands.FirstOrDefaultAsync(b => b.Id == product.BrandId && b.IsActive, cancellationToken);

        if (product is null || brand is null || !visibleCategories.ContainsKey(product.CategoryId) || !product.Variants.Any(v => v.IsActive))
            return Result<StoreProductDetail>.NotFound("Ürün bulunamadı.");

        var path = new List<StoreCategoryRef>();
        for (int? id = product.CategoryId; id is not null; id = visibleCategories[id.Value].ParentId)
            path.Insert(0, new StoreCategoryRef(visibleCategories[id.Value].Name, visibleCategories[id.Value].Slug));

        return Result<StoreProductDetail>.Success(new StoreProductDetail(
            product.Id, product.Name, product.Slug, product.Description, brand.Name, brand.Slug, path, product.VatRate,
            product.Images.OrderBy(i => i.SortOrder).Select(i => media.ToPublicUrl(i.Path)).ToList(),
            product.Variants.Where(v => v.IsActive)
                .Select(v => new StoreVariantDto(v.Id, v.Price, v.ListPrice, v.StockQuantity > 0,
                    v.Attributes.Select(a => new VariantAttributeDto(a.Name, a.Value)).ToList()))
                .ToList()));
    }

    // Pasif bir kategorinin alt kategorileri de vitrinde görünmez.
    private async Task<Dictionary<int, Category>> VisibleCategoriesAsync(CancellationToken cancellationToken)
    {
        var all = (await categories.ListAsync(cancellationToken: cancellationToken)).ToDictionary(c => c.Id);
        return all.Values
            .Where(c => IsVisible(c, all))
            .ToDictionary(c => c.Id);
    }

    private static bool IsVisible(Category category, IReadOnlyDictionary<int, Category> all)
    {
        for (Category? current = category; current is not null; current = current.ParentId is { } p && all.TryGetValue(p, out var parent) ? parent : null)
        {
            if (!current.IsActive)
                return false;
        }
        return true;
    }

    private static IEnumerable<int> Descendants(IReadOnlyList<Category> all, int rootId)
    {
        yield return rootId;
        foreach (var child in all.Where(c => c.ParentId == rootId))
            foreach (var id in Descendants(all, child.Id))
                yield return id;
    }

    private static List<StoreCategoryDto> BuildTree(IReadOnlyList<Category> all, int? parentId) =>
        all.Where(c => c.ParentId == parentId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new StoreCategoryDto(c.Id, c.Name, c.Slug, BuildTree(all, c.Id)))
            .ToList();

    private sealed class CardRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public int BrandId { get; init; }
        public DateTime CreatedDate { get; init; }
        public decimal MinPrice { get; init; }
        public decimal? ListPrice { get; init; }
        public bool InStock { get; init; }
        public string? ImagePath { get; init; }
    }
}
