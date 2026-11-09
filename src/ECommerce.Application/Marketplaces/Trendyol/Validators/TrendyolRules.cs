using FluentValidation;
using Trendyol.Client.Products;

namespace ECommerce.Application.Marketplaces.Trendyol;

internal static class TrendyolRules
{
    public const int MaxItemsPerRequest = 1000;
    public const int MaxImages = 8;
    public const int MaxStock = 20000;
    public static readonly int[] VatRates = [0, 1, 10, 20];

    public static IRuleBuilderOptions<T, string> Barcode<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(40)
            .Matches("^[A-Za-z0-9._-]+$").WithMessage("Barkod yalnızca harf, rakam ve '.', '-', '_' içerebilir.");

    public static IRuleBuilderOptions<T, IReadOnlyList<TrendyolImage>?> Images<T>(this IRuleBuilder<T, IReadOnlyList<TrendyolImage>?> rule) =>
        rule.Must(images => images is null || images.Count <= MaxImages).WithMessage($"En fazla {MaxImages} görsel gönderilebilir.")
            .Must(images => images is null || images.All(i => IsHttps(i.Url))).WithMessage("Görsel adresleri https olmalıdır.");

    public static IRuleBuilderOptions<T, IReadOnlyList<TItem>> Batch<T, TItem>(this IRuleBuilder<T, IReadOnlyList<TItem>> rule) =>
        rule.NotEmpty().Must(items => items.Count <= MaxItemsPerRequest).WithMessage($"Bir istekte en fazla {MaxItemsPerRequest} kalem gönderilebilir.");

    public static bool UniqueBarcodes(IEnumerable<string> barcodes) =>
        barcodes.GroupBy(b => b, StringComparer.Ordinal).All(g => g.Count() == 1);

    private static bool IsHttps(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
