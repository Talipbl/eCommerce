using ECommerce.AdminPanel.Infrastructure;

namespace ECommerce.AdminPanel.Models;

/// <summary>Marka veya kategori eşleme listesinde bir yerel kayıt ve Trendyol karşılığı (eşlenmediyse boş).</summary>
public sealed record MappingRow(int LocalId, string LocalName, int Depth, long? ExternalId, string? ExternalName, int AttributeCount = 0)
{
    public bool IsMapped => ExternalId is not null;

    public string IndentedName => new CategoryOption(LocalId, LocalName, Depth, true, []).IndentedName;
}
