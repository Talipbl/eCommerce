using ECommerce.AdminPanel.Api;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ECommerce.AdminPanel.Models;

/// <summary>
/// Kategori özellik eşleme sayfası. <see cref="Rows"/> ile <see cref="Attributes"/> aynı sıradadır. API eşlemeyi topluca değiştirdiği için
/// eşlenmiş her özellik, "tümünü göster" kapalıyken de sayfada bulunur; aksi halde kaydetmek onu silerdi.
/// </summary>
public sealed record AttributeMappingPage(
    MappingRow Category,
    IReadOnlyList<TrendyolAttribute> Attributes,
    IReadOnlyList<AttributeMappingRow> Rows,
    bool ShowAll,
    int HiddenOptionalCount)
{
    public const int BlankValueRows = 3;

    public static IReadOnlyList<AttributeMappingRow> RowsFrom(IReadOnlyList<TrendyolAttribute> attributes, CategoryMappingDto mapping) =>
        attributes.Select(a =>
        {
            var existing = mapping.Attributes.FirstOrDefault(m => m.ExternalAttributeId == a.Attribute.Id);
            var values = (existing?.Values ?? [])
                .Select(v => new ValueMappingRow { LocalValue = v.LocalValue, Value = a.Display(v.ValueId, v.CustomValue) })
                .Concat(Enumerable.Range(0, BlankValueRows).Select(_ => new ValueMappingRow()))
                .ToList();
            return new AttributeMappingRow
            {
                ExternalAttributeId = a.Attribute.Id,
                LocalAttributeName = existing?.LocalAttributeName,
                DefaultValue = existing is null ? null : a.Display(existing.DefaultValueId, existing.DefaultCustomValue),
                Values = values
            };
        }).ToList();

    /// <summary>Doldurulmuş satırları API isteğine çevirir; listede olmayan değerler <paramref name="modelState"/>'e ilgili alan için yazılır.</summary>
    public static SetCategoryAttributesRequest ToRequest(
        IReadOnlyList<TrendyolAttribute> attributes, IReadOnlyList<AttributeMappingRow> rows, ModelStateDictionary modelState)
    {
        var result = new List<CategoryAttributeMapping>();
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var attribute = attributes.FirstOrDefault(a => a.Attribute.Id == row.ExternalAttributeId);
            if (attribute is null || row.IsEmpty)
                continue;

            long? defaultId = null;
            string? defaultCustom = null;
            if (!string.IsNullOrWhiteSpace(row.DefaultValue) && !attribute.TryResolve(row.DefaultValue, out defaultId, out defaultCustom))
                modelState.AddModelError($"Rows[{i}].{nameof(row.DefaultValue)}", attribute.UnknownValueError(row.DefaultValue));

            var values = new List<AttributeValueMapping>();
            var isVariant = !string.IsNullOrWhiteSpace(row.LocalAttributeName);
            for (var j = 0; isVariant && j < row.Values.Count; j++)
            {
                var value = row.Values[j];
                if (string.IsNullOrWhiteSpace(value.LocalValue) || string.IsNullOrWhiteSpace(value.Value))
                    continue;
                if (attribute.TryResolve(value.Value, out var valueId, out var custom))
                    values.Add(new AttributeValueMapping { LocalValue = value.LocalValue.Trim(), ValueId = valueId, CustomValue = custom });
                else
                    modelState.AddModelError($"Rows[{i}].Values[{j}].{nameof(value.Value)}", attribute.UnknownValueError(value.Value));
            }

            result.Add(new CategoryAttributeMapping
            {
                ExternalAttributeId = attribute.Attribute.Id,
                LocalAttributeName = isVariant ? row.LocalAttributeName!.Trim() : null,
                DefaultValueId = defaultId,
                DefaultCustomValue = defaultCustom,
                Values = values
            });
        }

        return new SetCategoryAttributesRequest { Attributes = result };
    }
}
