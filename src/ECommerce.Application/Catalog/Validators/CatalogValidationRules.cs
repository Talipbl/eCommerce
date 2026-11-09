using ECommerce.Core.Text;
using FluentValidation;

namespace ECommerce.Application.Catalog;

internal static class CatalogValidationRules
{
    public static readonly int[] VatRates = [0, 1, 10, 20];

    public static IRuleBuilderOptions<T, string?> OptionalSlug<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(s => s is null || Slug.IsValid(s)).WithMessage("Adres (slug) yalnızca küçük harf, rakam ve tire içerebilir.");

    // Barkodlar pazaryerine de gönderileceği için Trendyol kuralları uygulanır.
    public static IRuleBuilderOptions<T, string> Barcode<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(40).Matches("^[A-Za-z0-9._-]+$")
            .WithMessage("Barkod yalnızca harf, rakam ve '.', '-', '_' içerebilir.");

    public static void Price<T>(this AbstractValidator<T> validator, Func<T, decimal> price, Func<T, decimal?> listPrice, Func<T, int> stock)
    {
        validator.RuleFor(x => price(x)).GreaterThan(0).OverridePropertyName("Price");
        validator.RuleFor(x => listPrice(x)).Must((x, l) => l is null || l >= price(x))
            .WithMessage("Liste fiyatı satış fiyatından düşük olamaz.").OverridePropertyName("ListPrice");
        validator.RuleFor(x => stock(x)).GreaterThanOrEqualTo(0).OverridePropertyName("StockQuantity");
    }
}
