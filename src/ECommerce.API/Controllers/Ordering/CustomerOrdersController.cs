using Asp.Versioning;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Common;
using ECommerce.Application.Ordering;
using ECommerce.Ordering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Ordering;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Policy = SecurityRegistration.CustomerPolicy)]
[Route("api/v{version:apiVersion}/customer")]
[Tags("Müşteri - Sipariş")]
public class CustomerOrdersController(ICheckoutService checkout, IOrderManagementService orders) : ControllerBase
{
    [HttpGet("checkout/preview")]
    public async Task<IActionResult> Preview([FromQuery] PaymentMethod paymentMethod, CancellationToken cancellationToken) =>
        this.ToActionResult(await checkout.PreviewAsync(User.GetAccountId(), paymentMethod, cancellationToken), Ok);

    [HttpPost("orders")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> PlaceOrder(CheckoutRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await checkout.PlaceOrderAsync(User.GetAccountId(), request, cancellationToken),
            o => CreatedAtAction(nameof(Get), new { orderNumber = o.OrderNumber, version = "1" }, o));

    [HttpGet("orders")]
    public async Task<PagedResult<OrderSummaryDto>> List([FromQuery] int page = 0, [FromQuery] int size = 20, CancellationToken cancellationToken = default) =>
        await orders.ListForCustomerAsync(User.GetAccountId(), page, size, cancellationToken);

    [HttpGet("orders/{orderNumber}")]
    public async Task<IActionResult> Get(string orderNumber, CancellationToken cancellationToken) =>
        this.ToActionResult(await orders.GetForCustomerAsync(User.GetAccountId(), orderNumber, cancellationToken), Ok);

    [HttpPost("orders/{orderNumber}/cancel")]
    public async Task<IActionResult> Cancel(string orderNumber, CancellationToken cancellationToken) =>
        this.ToActionResult(await orders.CancelByCustomerAsync(User.GetAccountId(), orderNumber, cancellationToken), Ok);
}
