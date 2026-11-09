namespace ECommerce.AdminPanel.Models;

/// <summary>Ürün listesi filtreleri; sorgu dizesinden okunur, sayfa 0'dan başlar.</summary>
public sealed record ProductFilter(string? Search, int? CategoryId, int? BrandId, bool? IsActive, int Page);
