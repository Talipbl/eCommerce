using ECommerce.Shopping;
using FluentValidation;

namespace ECommerce.Application.Shopping;

public class AddCartItemRequestValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartItemRequestValidator()
    {
        RuleFor(x => x.VariantId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, Cart.MaxQuantityPerLine);
    }
}

public class UpdateCartItemRequestValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartItemRequestValidator() => RuleFor(x => x.Quantity).InclusiveBetween(0, Cart.MaxQuantityPerLine);
}
