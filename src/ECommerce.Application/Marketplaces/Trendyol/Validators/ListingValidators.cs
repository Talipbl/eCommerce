using FluentValidation;

namespace ECommerce.Application.Marketplaces.Trendyol;

public class PublishListingRequestValidator : AbstractValidator<PublishListingRequest>
{
    public PublishListingRequestValidator()
    {
        RuleForEach(x => x.ProductAttributes).Must(a => a.AttributeId > 0 && (a.ValueId is not null || !string.IsNullOrWhiteSpace(a.CustomValue)))
            .WithMessage("Her özellik için değer numarası veya serbest değer girilmelidir.");
        RuleFor(x => x.DeliveryDuration).InclusiveBetween(1, 30).When(x => x.DeliveryDuration is not null);
    }
}
