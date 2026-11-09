using FluentValidation;

namespace ECommerce.Application.Catalog;

public class VariantInputValidator : AbstractValidator<VariantInput>
{
    public VariantInputValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Barcode).Barcode();
        this.Price(x => x.Price, x => x.ListPrice, x => x.StockQuantity);
        RuleFor(x => x.Attributes).NotNull();
        RuleForEach(x => x.Attributes).ChildRules(a =>
        {
            a.RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
            a.RuleFor(x => x.Value).NotEmpty().MaximumLength(100);
        });
    }
}

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).OptionalSlug();
        RuleFor(x => x.ProductCode).NotEmpty().MaximumLength(40);
        RuleFor(x => x.BrandId).GreaterThan(0);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.VatRate).Must(v => CatalogValidationRules.VatRates.Contains(v)).WithMessage("KDV oranı 0, 1, 10 veya 20 olmalıdır.");
        RuleFor(x => x.Variants).NotEmpty().Must(v => v.Count <= 100).WithMessage("Bir ürüne en fazla 100 varyant eklenebilir.")
            .Must(v => v.Select(x => x.Barcode).Distinct().Count() == v.Count && v.Select(x => x.Sku).Distinct().Count() == v.Count)
            .WithMessage("Varyantlarda stok kodu ve barkod tekrar edemez.");
        RuleForEach(x => x.Variants).SetValidator(new VariantInputValidator());
    }
}

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().OptionalSlug();
        RuleFor(x => x.ProductCode).NotEmpty().MaximumLength(40);
        RuleFor(x => x.VatRate).Must(v => CatalogValidationRules.VatRates.Contains(v)).WithMessage("KDV oranı 0, 1, 10 veya 20 olmalıdır.");
    }
}

public class UpdateVariantRequestValidator : AbstractValidator<UpdateVariantRequest>
{
    public UpdateVariantRequestValidator()
    {
        this.Price(x => x.Price, x => x.ListPrice, x => x.StockQuantity);
        RuleFor(x => x.Attributes).NotNull();
    }
}
