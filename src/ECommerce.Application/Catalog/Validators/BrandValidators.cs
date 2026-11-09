using FluentValidation;

namespace ECommerce.Application.Catalog;

public class SaveBrandRequestValidator : AbstractValidator<SaveBrandRequest>
{
    public SaveBrandRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Slug).OptionalSlug();
    }
}
