using ECommerce.AdminPanel.Api;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.AdminPanel.Models;

/// <summary>Yeni varyant formu. Stok kodu ve barkod sonradan değişmez; güncelleme <see cref="VariantUpdateForm"/> ile yapılır.</summary>
public sealed class VariantForm : IValidatableObject
{
    [Required(ErrorMessage = "Stok kodu zorunludur.")]
    [Display(Name = "Stok kodu (SKU)")]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "Barkod zorunludur.")]
    [Display(Name = "Barkod")]
    public string Barcode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Fiyat zorunludur.")]
    [Display(Name = "Satış fiyatı (₺)")]
    public decimal? Price { get; set; }

    [Display(Name = "Liste fiyatı (₺)", Prompt = "Üstü çizili fiyat, isteğe bağlı")]
    public decimal? ListPrice { get; set; }

    [Display(Name = "Stok")]
    public int StockQuantity { get; set; }

    [Display(Name = "Özellikler", Prompt = "Beden: M")]
    public string? AttributesText { get; set; }

    public VariantInput ToInput()
    {
        VariantAttributesText.TryParse(AttributesText, out var attributes);
        return new VariantInput
        {
            Sku = Sku.Trim(),
            Barcode = Barcode.Trim(),
            Price = Price!.Value,
            ListPrice = ListPrice,
            StockQuantity = StockQuantity,
            Attributes = attributes
        };
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!VariantAttributesText.TryParse(AttributesText, out _))
            yield return new ValidationResult(VariantAttributesText.FormatError, [nameof(AttributesText)]);
    }
}
