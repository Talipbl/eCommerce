using ECommerce.Application.Common;
using ECommerce.Ordering;

namespace ECommerce.Application.Ordering;

public interface ICheckoutService
{
    Task<Result<CheckoutPreviewDto>> PreviewAsync(int customerId, PaymentMethod paymentMethod, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> PlaceOrderAsync(int customerId, CheckoutRequest request, CancellationToken cancellationToken = default);
}
