namespace ECommerce.Application.Catalog;

public record SaveBrandRequest(string Name, string? Slug, bool IsActive);

public record BrandDto(int Id, string Name, string Slug, bool IsActive);
