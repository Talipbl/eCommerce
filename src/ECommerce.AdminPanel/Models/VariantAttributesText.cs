using ECommerce.AdminPanel.Api;

namespace ECommerce.AdminPanel.Models;

/// <summary>Varyant özellikleri formda satır başına "Ad: Değer" olarak yazılır (ör. "Beden: M").</summary>
public static class VariantAttributesText
{
    public const string FormatError = "Her satır \"Ad: Değer\" biçiminde olmalı.";

    public static string Format(IEnumerable<VariantAttributeDto>? attributes) =>
        string.Join("\n", (attributes ?? []).Select(a => $"{a.Name}: {a.Value}"));

    public static bool TryParse(string? text, out List<VariantAttributeDto> attributes)
    {
        attributes = [];
        foreach (var line in (text ?? string.Empty).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = line.IndexOf(':');
            var name = separator > 0 ? line[..separator].Trim() : string.Empty;
            var value = separator > 0 ? line[(separator + 1)..].Trim() : string.Empty;
            if (name.Length == 0 || value.Length == 0)
                return false;
            attributes.Add(new VariantAttributeDto { Name = name, Value = value });
        }

        return true;
    }

    /// <summary>API'nin varyant alan adlarını form alanlarına çevirir: <c>Attributes[0].Name</c> → <c>AttributesText</c>.</summary>
    public static string MapApiKey(string key) =>
        key.StartsWith("Attributes", StringComparison.OrdinalIgnoreCase) ? nameof(VariantForm.AttributesText) : key;
}
