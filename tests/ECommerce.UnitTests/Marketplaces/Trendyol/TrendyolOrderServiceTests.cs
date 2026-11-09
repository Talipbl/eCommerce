using ECommerce.Application.Marketplaces;
using ECommerce.Application.Marketplaces.Trendyol;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Trendyol.Client;
using Trendyol.Client.Orders;

namespace ECommerce.UnitTests.Marketplaces.Trendyol;

public class TrendyolOrderServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
    private readonly ITrendyolOrderClient _client = Substitute.For<ITrendyolOrderClient>();
    private readonly TrendyolOrderService _service;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public TrendyolOrderServiceTests()
    {
        _service = new TrendyolOrderService(_client, new FakeTimeProvider(Now));
        var ok = TrendyolResponse<TrendyolEmpty>.Success(200, null);
        _client.UpdatePackageStatusAsync(default, default!, default).ReturnsForAnyArgs(ok);
        _client.ReportUnsuppliedAsync(default, default!, default).ReturnsForAnyArgs(ok);
        _client.SendInvoiceLinkAsync(default!, default).ReturnsForAnyArgs(ok);
        _client.DeleteInvoiceLinkAsync(default!, default).ReturnsForAnyArgs(ok);
        _client.GetShipmentPackagesAsync(default!, default)
            .ReturnsForAnyArgs(TrendyolResponse<ShipmentPackagePage>.Success(200, new ShipmentPackagePage(0, 50, 0, 0, [])));
    }

    [Fact]
    public async Task Packages_query_is_forwarded()
    {
        var result = await _service.GetPackagesAsync(new OrderPackagesQuery(Now.AddDays(-7), Now, ShipmentPackageStatuses.Created, null, null, 0, 200), Ct);

        result.Succeeded.ShouldBeTrue();
        await _client.Received(1).GetShipmentPackagesAsync(
            Arg.Is<ShipmentPackageQuery>(q => q.StartDate == Now.AddDays(-7) && q.EndDate == Now && q.Status == "Created" && q.Size == 200),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(15, 0, 50, "Created")]
    [InlineData(7, 0, 201, "Created")]
    [InlineData(7, 50, 200, "Created")]
    [InlineData(7, 0, 50, "Bilinmeyen")]
    public async Task Invalid_package_queries_are_rejected_without_calling_trendyol(int days, int page, int size, string status)
    {
        var result = await _service.GetPackagesAsync(new OrderPackagesQuery(Now.AddDays(-days), Now, status, null, null, page, size), Ct);

        result.Error!.Kind.ShouldBe(MarketplaceErrorKind.Validation);
        await _client.DidNotReceiveWithAnyArgs().GetShipmentPackagesAsync(default!, Ct);
    }

    [Fact]
    public async Task Picking_sends_status_with_empty_params()
    {
        await _service.StartPickingAsync(10, new PackageLinesRequest([new PackageLine(555, 2)]), Ct);

        await _client.Received(1).UpdatePackageStatusAsync(10,
            Arg.Is<UpdatePackageStatusRequest>(r => r.Status == "Picking" && r.Params.Count == 0 && r.Lines.Single().LineId == 555),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoiced_sends_invoice_number()
    {
        await _service.MarkInvoicedAsync(10, new InvoicePackageRequest([new PackageLine(555, 2)], "EME2026000025208"), Ct);

        await _client.Received(1).UpdatePackageStatusAsync(10,
            Arg.Is<UpdatePackageStatusRequest>(r => r.Status == "Invoiced" && r.Params["invoiceNumber"] == "EME2026000025208"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unsupplied_sends_reason_code()
    {
        await _service.ReportUnsuppliedAsync(10, new UnsuppliedPackageRequest([new PackageLine(555, 1)], UnsuppliedReason.OutOfStock), Ct);

        await _client.Received(1).ReportUnsuppliedAsync(10, Arg.Is<UnsuppliedRequest>(r => r.ReasonId == 500), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoice_link_sends_milliseconds_and_maps_conflict()
    {
        _client.SendInvoiceLinkAsync(default!, Ct).ReturnsForAnyArgs(
            TrendyolResponse<TrendyolEmpty>.Failure(409, new TrendyolError(null, [new TrendyolErrorDetail(null, "Fatura zaten var", null)], "{}")));
        var invoiceDate = Now.AddHours(-1);

        var result = await _service.SendInvoiceLinkAsync(10, new SendInvoiceLinkCommand("https://fatura.example.com/1.pdf", "FRY2024567890123", invoiceDate), Ct);

        result.Error!.Kind.ShouldBe(MarketplaceErrorKind.Conflict);
        await _client.Received(1).SendInvoiceLinkAsync(
            Arg.Is<SendInvoiceLinkRequest>(r => r.ShipmentPackageId == 10 && r.InvoiceDateTime == invoiceDate.ToUnixTimeMilliseconds() && r.InvoiceNumber == "FRY2024567890123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoice_link_delete_uses_package_and_customer()
    {
        var result = await _service.DeleteInvoiceLinkAsync(10, customerId: 99, Ct);

        result.Succeeded.ShouldBeTrue();
        await _client.Received(1).DeleteInvoiceLinkAsync(Arg.Is<DeleteInvoiceLinkRequest>(r => r.ServiceSourceId == 10 && r.CustomerId == 99 && r.ChannelId == 1), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("http://fatura.example.com/1.pdf", null)]
    [InlineData("https://fatura.example.com/1.pdf", "FRY12345")]
    public void Invoice_link_rules(string link, string? invoiceNumber) =>
        new SendInvoiceLinkCommandValidator().Validate(new SendInvoiceLinkCommand(link, invoiceNumber, null)).IsValid.ShouldBeFalse();

    [Fact]
    public void Package_lines_must_be_positive_and_unique() =>
        new PackageLinesRequestValidator().Validate(new PackageLinesRequest([new PackageLine(1, 1), new PackageLine(1, 2)])).IsValid.ShouldBeFalse();
}
