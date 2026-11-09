using Trendyol.Client.Orders;

namespace Trendyol.Client.Tests;

public sealed class OrderClientContractTests : IDisposable
{
    private const string Orders = $"/integration/order/sellers/{TrendyolServer.SellerId}";
    private const string Invoices = $"/integration/sellers/{TrendyolServer.SellerId}/seller-invoice-links";

    private readonly TrendyolServer _api = new();
    private ITrendyolOrderClient Client => _api.Client<ITrendyolOrderClient>();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Shipment_packages_use_v2_path_and_millisecond_dates()
    {
        _api.Respond("GET", $"{Orders}/v2/orders", 200, """
            {"totalElements":1,"totalPages":1,"page":0,"size":200,"content":[
              {"shipmentPackageId":3330000001,"orderNumber":"10654411111","status":"Created","shipmentPackageStatus":"ReadyToShip",
               "orderDate":1793491200000,"customerId":99,"customerFirstName":"Ali","customerLastName":"Veli","currencyCode":"TRY",
               "packageGrossAmount":300,"packageTotalDiscount":50,"packageTotalPrice":250,"cargoProviderName":"Trendyol Express",
               "cargoTrackingNumber":7330000000000,
               "shipmentAddress":{"id":1,"fullName":"Ali Veli","city":"İstanbul","district":"Kadıköy","fullAddress":"Moda Cd. No:1"},
               "invoiceAddress":{"id":2,"fullName":"Ali Veli","city":"İstanbul","taxNumber":"1234567890"},
               "lines":[{"lineId":555,"barcode":"BRK-1","productName":"Tişört","quantity":2,"lineUnitPrice":150,"lineGrossAmount":300,
                 "lineTotalDiscount":50,"vatRate":20,"currencyCode":"TRY","orderLineItemStatusName":"Created"}],
               "packageHistories":[{"createdDate":1793491200000,"status":"Created"}]}]}
            """);
        var start = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddDays(7);

        var result = await Client.GetShipmentPackagesAsync(new ShipmentPackageQuery
        {
            StartDate = start,
            EndDate = end,
            Status = ShipmentPackageStatuses.Created,
            Size = 200
        }, Ct);

        var query = _api.SingleRequest().Query!;
        query["startDate"].ShouldContain(start.ToUnixTimeMilliseconds().ToString());
        query["endDate"].ShouldContain(end.ToUnixTimeMilliseconds().ToString());
        query["status"].ShouldContain("Created");
        query["size"].ShouldContain("200");

        var package = result.Data!.Content.ShouldHaveSingleItem();
        package.ShipmentPackageId.ShouldBe(3330000001);
        package.ShipmentAddress!.City.ShouldBe("İstanbul");
        package.InvoiceAddress!.TaxNumber.ShouldBe("1234567890");
        var line = package.Lines.ShouldHaveSingleItem();
        line.LineId.ShouldBe(555);
        line.LineUnitPrice.ShouldBe(150m);
    }

    [Fact]
    public async Task Invoiced_status_sends_lines_status_and_invoice_number()
    {
        _api.Respond("PUT", $"{Orders}/shipment-packages/3330000001", 200);

        var result = await Client.UpdatePackageStatusAsync(3330000001, new UpdatePackageStatusRequest(
            [new PackageLine(555, 2)], ShipmentPackageStatuses.Invoiced, new Dictionary<string, string> { ["invoiceNumber"] = "EME2026000025208" }), Ct);

        result.IsSuccess.ShouldBeTrue();
        TrendyolServer.ShouldBeJson(_api.SingleRequest().Body,
            """{"lines":[{"lineId":555,"quantity":2}],"status":"Invoiced","params":{"invoiceNumber":"EME2026000025208"}}""");
    }

    [Fact]
    public async Task Unsupplied_sends_reason_id()
    {
        _api.Respond("PUT", $"{Orders}/shipment-packages/3330000001/items/unsupplied", 200);

        await Client.ReportUnsuppliedAsync(3330000001, new UnsuppliedRequest([new PackageLine(555, 1)], 500), Ct);

        TrendyolServer.ShouldBeJson(_api.SingleRequest().Body, """{"lines":[{"lineId":555,"quantity":1}],"reasonId":500}""");
    }

    [Fact]
    public async Task Invoice_link_is_sent()
    {
        _api.Respond("POST", Invoices, 201);

        var result = await Client.SendInvoiceLinkAsync(new SendInvoiceLinkRequest("https://fatura.example.com/1.pdf", 3330000001), Ct);

        result.IsSuccess.ShouldBeTrue();
        TrendyolServer.ShouldBeJson(_api.SingleRequest().Body,
            """{"invoiceLink":"https://fatura.example.com/1.pdf","shipmentPackageId":3330000001}""");
    }

    [Fact]
    public async Task Invoice_link_conflict_is_reported()
    {
        _api.Respond("POST", Invoices, 409, """{"errors":[{"key":"invoice","message":"Pakete ait fatura zaten var"}]}""");

        var result = await Client.SendInvoiceLinkAsync(new SendInvoiceLinkRequest("https://fatura.example.com/1.pdf", 1), Ct);

        result.StatusCode.ShouldBe(409);
        result.Error!.Messages.ShouldBe(["Pakete ait fatura zaten var"]);
    }

    [Fact]
    public async Task Invoice_link_delete_sends_channel_and_customer()
    {
        _api.Respond("POST", $"{Invoices}/delete", 202);

        await Client.DeleteInvoiceLinkAsync(new DeleteInvoiceLinkRequest(3330000001, 99), Ct);

        TrendyolServer.ShouldBeJson(_api.SingleRequest().Body, """{"serviceSourceId":3330000001,"customerId":99,"channelId":1}""");
    }

    public void Dispose() => _api.Dispose();
}
