using Asp.Versioning;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Common;
using ECommerce.Application.Ordering;
using ECommerce.Ordering;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Ordering;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/orders")]
[Tags("Siparişler")]
public class OrdersController(IOrderManagementService orders) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<OrderSummaryDto>> List(
        [FromQuery] OrderStatus? status, [FromQuery] OrderChannel? channel, [FromQuery] string? search,
        [FromQuery] int page = 0, [FromQuery] int size = 20, CancellationToken cancellationToken = default) =>
        await orders.ListAsync(new OrderAdminQuery(status, channel, search, page, size), cancellationToken);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> Get(int id, CancellationToken cancellationToken) =>
        this.ToOk(await orders.GetAsync(id, cancellationToken));

    [HttpPost("{id:int}/confirm-payment")]
    public async Task<ActionResult<OrderDto>> ConfirmPayment(int id, CancellationToken cancellationToken) =>
        this.ToOk(await orders.ConfirmPaymentAsync(id, cancellationToken));

    [HttpPost("{id:int}/prepare")]
    public async Task<ActionResult<OrderDto>> StartPreparing(int id, CancellationToken cancellationToken) =>
        this.ToOk(await orders.StartPreparingAsync(id, cancellationToken));

    [HttpPost("{id:int}/ship")]
    public async Task<ActionResult<OrderDto>> Ship(int id, ShipOrderRequest request, CancellationToken cancellationToken) =>
        this.ToOk(await orders.ShipAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/deliver")]
    public async Task<ActionResult<OrderDto>> Deliver(int id, CancellationToken cancellationToken) =>
        this.ToOk(await orders.DeliverAsync(id, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(int id, CancelOrderRequest request, CancellationToken cancellationToken) =>
        this.ToOk(await orders.CancelAsync(id, request.Reason, cancellationToken));
}
