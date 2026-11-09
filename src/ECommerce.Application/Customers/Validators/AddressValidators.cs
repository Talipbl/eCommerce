using FluentValidation;

namespace ECommerce.Application.Customers;

public class SaveAddressRequestValidator : AbstractValidator<SaveAddressRequest>
{
    public SaveAddressRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20).Matches("^[0-9+ ()-]+$").WithMessage("Telefon numarası geçersiz.");
        RuleFor(x => x.City).NotEmpty().MaximumLength(50);
        RuleFor(x => x.District).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AddressLine).NotEmpty().MaximumLength(300);
        RuleFor(x => x.PostalCode).MaximumLength(10);
    }
}
