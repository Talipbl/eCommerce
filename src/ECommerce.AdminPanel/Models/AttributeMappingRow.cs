namespace ECommerce.AdminPanel.Models;

/// <summary>
/// Bir Trendyol kategori özelliğinin formdaki satırı. <see cref="LocalAttributeName"/> boşsa ürün için sabit <see cref="DefaultValue"/> gönderilir;
/// doluysa varyantın o özelliği <see cref="Values"/> ile çevrilir, karşılığı olmayan değerde varsayılana düşülür.
/// </summary>
public sealed class AttributeMappingRow
{
    public long ExternalAttributeId { get; set; }

    public string? LocalAttributeName { get; set; }

    public string? DefaultValue { get; set; }

    public List<ValueMappingRow> Values { get; set; } = [];

    public bool IsEmpty => string.IsNullOrWhiteSpace(LocalAttributeName) && string.IsNullOrWhiteSpace(DefaultValue);
}
