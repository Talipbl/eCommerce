using ECommerce.Core.Domain;
using ECommerce.Core.Text;

namespace ECommerce.Catalog;

internal static class CatalogRules
{
    public static readonly int[] VatRates = [0, 1, 10, 20];

    public static string RequiredText(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} boş olamaz.");
        if (value.Trim().Length > maxLength)
            throw new DomainException($"{field} en fazla {maxLength} karakter olabilir.");
        return value.Trim();
    }

    public static string ValidSlug(string slug) =>
        Slug.IsValid(slug) ? slug : throw new DomainException("Slug yalnızca küçük harf, rakam ve tire içerebilir.");
}
