using FluentValidation;

namespace ECommerce.Application.Ordering;

public class ShipOrderRequestValidator : AbstractValidator<ShipOrderRequest>
{
    public ShipOrderRequestValidator()
    {
        RuleFor(x => x.CargoProvider).MaximumLength(100);
        RuleFor(x => x.TrackingNumber).MaximumLength(100);
        RuleFor(x => x.InvoiceNumber).MaximumLength(50);
    }
}

public class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
}
