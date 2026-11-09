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
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken) =>
        this.ToActionResult(await orders.GetAsync(id, cancellationToken), Ok);

    [HttpPost("{id:int}/confirm-payment")]
    public async Task<IActionResult> ConfirmPayment(int id, CancellationToken cancellationToken) =>
        this.ToActionResult(await orders.ConfirmPaymentAsync(id, cancellationToken), Ok);

    [HttpPost("{id:int}/prepare")]
    public async Task<IActionResult> StartPreparing(int id, CancellationToken cancellationToken) =>
        this.ToActionResult(await orders.StartPreparingAsync(id, cancellationToken), Ok);

    [HttpPost("{id:int}/ship")]
    public async Task<IActionResult> Ship(int id, ShipOrderRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await orders.ShipAsync(id, request, cancellationToken), Ok);

    [HttpPost("{id:int}/deliver")]
    public async Task<IActionResult> Deliver(int id, CancellationToken cancellationToken) =>
        this.ToActionResult(await orders.DeliverAsync(id, cancellationToken), Ok);

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancelOrderRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await orders.CancelAsync(id, request.Reason, cancellationToken), Ok);
}
