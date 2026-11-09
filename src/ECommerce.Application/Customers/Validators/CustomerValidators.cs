using FluentValidation;

namespace ECommerce.Application.Customers;

public class RegisterCustomerRequestValidator : AbstractValidator<RegisterCustomerRequest>
{
    public RegisterCustomerRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Za-zÇĞİÖŞÜçğıöşü]").WithMessage("Parola en az bir harf içermelidir.")
            .Matches("[0-9]").WithMessage("Parola en az bir rakam içermelidir.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}

public class UpdateCustomerProfileRequestValidator : AbstractValidator<UpdateCustomerProfileRequest>
{
    public UpdateCustomerProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}
