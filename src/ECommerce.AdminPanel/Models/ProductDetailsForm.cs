using ECommerce.AdminPanel.Api;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.AdminPanel.Models;

/// <summary>Ürünün varyant dışı bilgileri; oluşturma ve düzenleme formlarında ortaktır.</summary>
public sealed class ProductDetailsForm
{
    public static readonly int[] VatRates = [20, 10, 1, 0];

    [Required(ErrorMessage = "Ürün adı zorunludur.")]
    [Display(Name = "Ürün adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Slug", Prompt = "Boş bırakılırsa addan üretilir")]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "Ürün kodu zorunludur.")]
    [Display(Name = "Ürün kodu")]
    public string ProductCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Marka seçin.")]
    [Display(Name = "Marka")]
    public int? BrandId { get; set; }

    [Required(ErrorMessage = "Kategori seçin.")]
    [Display(Name = "Kategori")]
    public int? CategoryId { get; set; }

    [Display(Name = "KDV oranı (%)")]
    public int VatRate { get; set; } = 20;

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    public static ProductDetailsForm From(ProductDetailDto product) => new()
    {
        Name = product.Name,
        Slug = product.Slug,
        ProductCode = product.ProductCode,
        BrandId = product.BrandId,
        CategoryId = product.CategoryId,
        VatRate = product.VatRate,
        Description = product.Description
    };

    public UpdateProductRequest ToUpdateRequest() => new()
    {
        Name = Name,
        Slug = Slug?.Trim() ?? string.Empty,
        ProductCode = ProductCode,
        BrandId = BrandId!.Value,
        CategoryId = CategoryId!.Value,
        VatRate = VatRate,
        Description = Normalize(Description)
    };

    public CreateProductRequest ToCreateRequest(VariantInput variant) => new()
    {
        Name = Name,
        Slug = Normalize(Slug),
        ProductCode = ProductCode,
        BrandId = BrandId!.Value,
        CategoryId = CategoryId!.Value,
        VatRate = VatRate,
        Description = Normalize(Description),
        Variants = [variant]
    };

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
