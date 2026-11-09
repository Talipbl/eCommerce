namespace ECommerce.Application.Marketplaces.Trendyol;

public record CategoryNodeDto(long Id, string Name, long? ParentId, bool IsLeaf, IReadOnlyList<CategoryNodeDto> SubCategories);

public record CategorySearchResultDto(long Id, string Name, string Path);

public record CategoryAttributeDto(long Id, string Name, bool Required, bool AllowCustom, bool Varianter, bool Slicer, bool AllowMultipleValues);

public record AttributeValueDto(long Id, string Value);

public record TrendyolBrandDto(long Id, string Name);
