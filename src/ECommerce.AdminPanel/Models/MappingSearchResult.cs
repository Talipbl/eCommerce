namespace ECommerce.AdminPanel.Models;

/// <summary>Trendyol arama sonucu; kategori için tam yol (Giyim > Kadın > Tişört) gösterilir.</summary>
public sealed record MappingSearchResult(long Id, string Name, string? Path);
