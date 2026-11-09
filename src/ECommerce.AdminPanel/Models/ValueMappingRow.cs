namespace ECommerce.AdminPanel.Models;

/// <summary>Yerel varyant değeri (ör. "M") → Trendyol değerinin adı (ör. "M / 38"); ad sunucuda değer numarasına çevrilir.</summary>
public sealed class ValueMappingRow
{
    public string? LocalValue { get; set; }

    public string? Value { get; set; }
}
