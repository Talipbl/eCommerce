namespace ECommerce.Application.Catalog;

public enum StoreProductSort
{
    Newest,
    PriceAscending,
    PriceDescending,
    Name
}

public record StoreProductQuery
{
    public string? CategorySlug { get; init; }
    public string? BrandSlug { get; init; }
    public string? Search { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool InStockOnly { get; init; }
    public StoreProductSort Sort { get; init; } = StoreProductSort.Newest;
    public int Page { get; init; }
    public int Size { get; init; } = 24;
}

public record StoreCategoryDto(int Id, string Name, string Slug, IReadOnlyList<StoreCategoryDto> Children);

public record StoreCategoryRef(string Name, string Slug);

public record StoreProductCard(int Id, string Name, string Slug, string BrandName, decimal MinPrice, decimal? ListPrice, bool InStock, string? ImageUrl);

public record StoreVariantDto(int Id, decimal Price, decimal? ListPrice, bool InStock, IReadOnlyList<VariantAttributeDto> Attributes);

public record StoreProductDetail(
    int Id,
    string Name,
    string Slug,
    string? Description,
    string BrandName,
    string BrandSlug,
    IReadOnlyList<StoreCategoryRef> CategoryPath,
    int VatRate,
    IReadOnlyList<string> Images,
    IReadOnlyList<StoreVariantDto> Variants);
