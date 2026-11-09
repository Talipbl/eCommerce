using ECommerce.AdminPanel.Api;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.AdminPanel.Models;

public sealed class VariantUpdateForm : IValidatableObject
{
    [Required(ErrorMessage = "Fiyat zorunludur.")]
    public decimal? Price { get; set; }

    public decimal? ListPrice { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }

    public string? AttributesText { get; set; }

    public UpdateVariantRequest ToRequest()
    {
        VariantAttributesText.TryParse(AttributesText, out var attributes);
        return new UpdateVariantRequest
        {
            Price = Price!.Value,
            ListPrice = ListPrice,
            StockQuantity = StockQuantity,
            IsActive = IsActive,
            Attributes = attributes
        };
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!VariantAttributesText.TryParse(AttributesText, out _))
            yield return new ValidationResult(VariantAttributesText.FormatError, [nameof(AttributesText)]);
    }
}
