namespace ECommerce.AdminPanel.Models;

/// <summary>Sayfa numarası 0'dan başlar (API ile aynı); ekranda 1'den gösterilir.</summary>
public sealed record PagerModel(int Page, int TotalPages, int TotalElements);
