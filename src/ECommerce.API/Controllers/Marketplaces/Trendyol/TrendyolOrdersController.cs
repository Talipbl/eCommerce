using Asp.Versioning;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Marketplaces.Trendyol;
using Microsoft.AspNetCore.Mvc;
using Trendyol.Client.Orders;

namespace ECommerce.API.Controllers.Marketplaces.Trendyol;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/marketplaces/trendyol/orders/packages")]
[Tags("Trendyol - Siparişler")]
public class TrendyolOrdersController(ITrendyolOrderService orders) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ShipmentPackagePage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ShipmentPackagePage>> List([FromQuery] OrderPackagesFilter filter, CancellationToken cancellationToken) =>
        this.ToOk(await orders.GetPackagesAsync(new OrderPackagesQuery(
            filter.StartDate, filter.EndDate, filter.Status, filter.OrderNumber, filter.ShipmentPackageId, filter.Page, filter.Size), cancellationToken));

    [HttpPost("{packageId:long}/picking")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> StartPicking(long packageId, PackageLinesRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await orders.StartPickingAsync(packageId, request, cancellationToken), _ => NoContent());

    [HttpPost("{packageId:long}/invoiced")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkInvoiced(long packageId, InvoicePackageRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await orders.MarkInvoicedAsync(packageId, request, cancellationToken), _ => NoContent());

    [HttpPost("{packageId:long}/unsupplied")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReportUnsupplied(long packageId, UnsuppliedPackageRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await orders.ReportUnsuppliedAsync(packageId, request, cancellationToken), _ => NoContent());

    [HttpPost("{packageId:long}/invoice-link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SendInvoiceLink(long packageId, SendInvoiceLinkCommand command, CancellationToken cancellationToken) =>
        this.ToActionResult(await orders.SendInvoiceLinkAsync(packageId, command, cancellationToken), _ => NoContent());

    [HttpDelete("{packageId:long}/invoice-link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteInvoiceLink(long packageId, [FromQuery] long customerId, CancellationToken cancellationToken) =>
        this.ToActionResult(await orders.DeleteInvoiceLinkAsync(packageId, customerId, cancellationToken), _ => NoContent());
}
