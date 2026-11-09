using ECommerce.AdminPanel.Api;
using System.Globalization;

namespace ECommerce.AdminPanel.Models;

/// <summary>Trendyol kategori özelliği ve değer listesi; değer adı ↔ numara çevirisi burada yapılır.</summary>
public sealed record TrendyolAttribute(CategoryAttributeDto Attribute, IReadOnlyList<AttributeValueDto> Values)
{
    private static readonly CompareInfo Turkish = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;

    public string DataListId => $"values-{Attribute.Id}";

    public string Display(long? valueId, string? customValue) =>
        valueId is { } id ? Values.FirstOrDefault(v => v.Id == id)?.Value ?? id.ToString(CultureInfo.InvariantCulture) : customValue ?? string.Empty;

    /// <summary>
    /// Yazılan metni değer listesinde arar (Türkçe, büyük/küçük harf duyarsız). Bulunamazsa özellik serbest değere izin veriyorsa
    /// metin serbest değer olur; izin vermiyorsa false döner.
    /// </summary>
    public bool TryResolve(string text, out long? valueId, out string? customValue)
    {
        var trimmed = text.Trim();
        var match = Values.FirstOrDefault(v => Turkish.Compare(v.Value.Trim(), trimmed, CompareOptions.IgnoreCase) == 0);
        valueId = match?.Id;
        customValue = match is null && Attribute.AllowCustom ? trimmed : null;
        return match is not null || Attribute.AllowCustom;
    }

    public string UnknownValueError(string text) => $"\"{text.Trim()}\" {Attribute.Name} için Trendyol değer listesinde yok; listeden seçin.";
}
