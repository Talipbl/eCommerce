using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;
using ECommerce.AdminPanel.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.AdminPanel.Controllers;

[Route("orders")]
public class OrdersController(IOrdersClient orders) : PanelController
{
    private const int PageSize = 20;

    [HttpGet("")]
    public async Task<IActionResult> Index(OrderStatus? status, OrderChannel? channel, string? search, int page, CancellationToken cancellationToken)
    {
        var filter = new OrderFilter(status, channel, string.IsNullOrWhiteSpace(search) ? null : search.Trim(), Math.Max(page, 0));
        var result = await orders.ListAsync(filter.Status, filter.Channel, filter.Search, filter.Page, PageSize, cancellationToken);
        return View(new OrderListPage(filter, result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var order = await FindAsync(id, cancellationToken);
        return order is null ? NotFound() : View(new OrderDetailPage(order, ShipOrderForm.From(order), new CancelOrderForm()));
    }

    [HttpPost("{id:int}/confirm-payment")]
    public Task<IActionResult> ConfirmPayment(int id, CancellationToken cancellationToken) =>
        RunAsync(id, () => orders.ConfirmPaymentAsync(id, cancellationToken), "Ödeme onaylandı.");

    [HttpPost("{id:int}/prepare")]
    public Task<IActionResult> Prepare(int id, CancellationToken cancellationToken) =>
        RunAsync(id, () => orders.StartPreparingAsync(id, cancellationToken), "Sipariş hazırlanıyor olarak işaretlendi.");

    [HttpPost("{id:int}/deliver")]
    public Task<IActionResult> Deliver(int id, CancellationToken cancellationToken) =>
        RunAsync(id, () => orders.DeliverAsync(id, cancellationToken), "Sipariş teslim edildi olarak işaretlendi.");

    [HttpPost("{id:int}/ship")]
    public async Task<IActionResult> Ship(int id, [Bind(Prefix = "Ship")] ShipOrderForm form, CancellationToken cancellationToken)
    {
        try
        {
            await orders.ShipAsync(id, form.ToRequest(), cancellationToken);
            FlashSuccess("Sipariş kargoya verildi.");
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (ApiException exception)
        {
            ModelState.AddApiProblem(exception, "Ship");
            return await DetailViewAsync(id, form, null, OrderDetailPage.ShipForm, cancellationToken);
        }
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [Bind(Prefix = "Cancel")] CancelOrderForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return await DetailViewAsync(id, null, form, OrderDetailPage.CancelForm, cancellationToken);

        try
        {
            await orders.CancelAsync(id, new CancelOrderRequest { Reason = form.Reason.Trim() }, cancellationToken);
            FlashSuccess("Sipariş iptal edildi.");
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (ApiException exception)
        {
            ModelState.AddApiProblem(exception, "Cancel");
            return await DetailViewAsync(id, null, form, OrderDetailPage.CancelForm, cancellationToken);
        }
    }

    private async Task<IActionResult> RunAsync(int id, Func<Task> action, string success)
    {
        try
        {
            await action();
            FlashSuccess(success);
        }
        catch (ApiException exception)
        {
            FlashError(exception);
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    private async Task<IActionResult> DetailViewAsync(int id, ShipOrderForm? ship, CancelOrderForm? cancel, string activeForm, CancellationToken cancellationToken)
    {
        var order = await FindAsync(id, cancellationToken);
        return order is null
            ? NotFound()
            : View(nameof(Detail), new OrderDetailPage(order, ship ?? ShipOrderForm.From(order), cancel ?? new CancelOrderForm(), activeForm));
    }

    private async Task<OrderDto?> FindAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await orders.GetAsync(id, cancellationToken);
        }
        catch (ApiException exception) when (exception.StatusCode == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }
}
