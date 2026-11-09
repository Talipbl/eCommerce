using FluentValidation;

namespace ECommerce.Application.Catalog;

public class SaveCategoryRequestValidator : AbstractValidator<SaveCategoryRequest>
{
    public SaveCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Slug).OptionalSlug();
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
