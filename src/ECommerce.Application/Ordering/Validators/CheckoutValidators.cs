using ECommerce.Ordering;
using FluentValidation;

namespace ECommerce.Application.Ordering;

public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.AddressId).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).IsInEnum().NotEqual(PaymentMethod.Marketplace);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
