namespace ECommerce.Application.Catalog;

public record SaveCategoryRequest(string Name, string? Slug, int? ParentId, string? Description, int SortOrder, bool IsActive);

public record CategoryDto(int Id, string Name, string Slug, int? ParentId, string? Description, int SortOrder, bool IsActive);

public record CategoryTreeDto(int Id, string Name, string Slug, int? ParentId, int SortOrder, bool IsActive, IReadOnlyList<CategoryTreeDto> Children);
