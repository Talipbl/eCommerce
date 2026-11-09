namespace ECommerce.Application.Catalog;

public record VariantAttributeDto(string Name, string Value);

public record VariantInput(string Sku, string Barcode, decimal Price, decimal? ListPrice, int StockQuantity, IReadOnlyList<VariantAttributeDto> Attributes);

public record CreateProductRequest(
    string Name,
    string? Slug,
    string ProductCode,
    int BrandId,
    int CategoryId,
    int VatRate,
    string? Description,
    IReadOnlyList<VariantInput> Variants);

public record UpdateProductRequest(string Name, string Slug, string ProductCode, int BrandId, int CategoryId, int VatRate, string? Description);

public record UpdateVariantRequest(decimal Price, decimal? ListPrice, int StockQuantity, bool IsActive, IReadOnlyList<VariantAttributeDto> Attributes);

public record ProductAdminQuery(string? Search, int? CategoryId, int? BrandId, bool? IsActive, int Page = 0, int Size = 20);

public record ProductListItemDto(
    int Id,
    string Name,
    string Slug,
    string ProductCode,
    int BrandId,
    int CategoryId,
    bool IsActive,
    int VariantCount,
    int TotalStock,
    decimal? MinPrice,
    string? ImageUrl);

public record VariantDto(int Id, string Sku, string Barcode, decimal Price, decimal? ListPrice, int StockQuantity, bool IsActive, IReadOnlyList<VariantAttributeDto> Attributes);

public record ProductImageDto(int Id, string Url, string? AltText, int SortOrder);

public record ProductDetailDto(
    int Id,
    string Name,
    string Slug,
    string ProductCode,
    int BrandId,
    int CategoryId,
    int VatRate,
    string? Description,
    bool IsActive,
    IReadOnlyList<VariantDto> Variants,
    IReadOnlyList<ProductImageDto> Images);

public record ReorderImagesRequest(IReadOnlyList<int> ImageIds);
