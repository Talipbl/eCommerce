using ECommerce.Marketplaces;

namespace ECommerce.Application.Marketplaces.Trendyol;

public record MapCategoryRequest(long ExternalCategoryId, string? ExternalCategoryName);

public record MapBrandRequest(long ExternalBrandId, string? ExternalBrandName);

public record CategoryMappingDto(int CategoryId, long ExternalCategoryId, string? ExternalCategoryName, IReadOnlyList<CategoryAttributeMapping> Attributes);

public record SetCategoryAttributesRequest(IReadOnlyList<CategoryAttributeMapping> Attributes);

public record BrandMappingDto(int BrandId, long ExternalBrandId, string? ExternalBrandName);
