namespace Trendyol.Client.Catalog;

public sealed record TrendyolCategoryTree(IReadOnlyList<TrendyolCategory>? Categories);

public sealed record TrendyolCategory(long Id, string Name, long? ParentId, IReadOnlyList<TrendyolCategory>? SubCategories)
{
    public bool IsLeaf => SubCategories is not { Count: > 0 };
}

public sealed record TrendyolCategoryAttributes(long Id, string? Name, string? DisplayName, IReadOnlyList<TrendyolCategoryAttribute>? CategoryAttributes);

public sealed record TrendyolCategoryAttribute(
    TrendyolAttributeInfo Attribute,
    long CategoryId,
    bool Required,
    bool AllowCustom,
    bool Varianter,
    bool Slicer,
    bool AllowMultipleAttributeValues);

public sealed record TrendyolAttributeInfo(long Id, string Name);

public sealed record TrendyolAttributeValuePage(int Page, int Size, int TotalElements, int TotalPages, IReadOnlyList<TrendyolAttributeValue>? Content);

public sealed record TrendyolAttributeValue(long AttributeValueId, string AttributeValue);

public sealed record TrendyolBrandList(IReadOnlyList<TrendyolBrand>? Brands);

public sealed record TrendyolBrand(long Id, string Name, bool Luxe);
