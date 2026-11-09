namespace ECommerce.AdminPanel.Models;

/// <summary>Bir yerel marka veya kategori için Trendyol'da arama; sonuçlardan biri seçilerek eşlenir.</summary>
public sealed record MappingSearchPage(
    MappingRow Local,
    string? Query,
    IReadOnlyList<MappingSearchResult> Results,
    string? Error);
