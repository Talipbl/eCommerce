using ECommerce.Application.Marketplaces;
using ECommerce.Application.Marketplaces.Trendyol;
using ECommerce.Marketplaces;
using ECommerce.Ordering;
using NSubstitute;
using Trendyol.Client.Orders;
using Trendyol.Client.Products;

namespace ECommerce.UnitTests.Marketplaces;

public class TrendyolMarketplaceProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);
    private readonly ITrendyolOrderService _orders = Substitute.For<ITrendyolOrderService>();
    private readonly ITrendyolProductService _products = Substitute.For<ITrendyolProductService>();
    private readonly ITrendyolListingService _listings = Substitute.For<ITrendyolListingService>();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private TrendyolMarketplaceProvider Provider => new(_listings, _products, _orders);

    [Fact]
    public void Limits_follow_trendyol_rules()
    {
        Provider.Name.ShouldBe(MarketplaceNames.Trendyol);
        Provider.Channel.ShouldBe(OrderChannel.Trendyol);
        Provider.MaxOrderWindow.ShouldBe(TimeSpan.FromDays(14));
        Provider.MaxOrderLookback.ShouldBeLessThan(TimeSpan.FromDays(29));
        Provider.MaxPriceInventoryItems.ShouldBe(1000);
        (Provider.MaxOrderPages * 200).ShouldBe(10000);
    }

    [Fact]
    public async Task Packages_are_read_by_last_modified_date_ascending_and_mapped_to_orders()
    {
        _orders.GetPackagesAsync(default!, Ct).ReturnsForAnyArgs(MarketplaceResult<ShipmentPackagePage>.Success(new ShipmentPackagePage(0, 200, 1, 1,
        [
            new ShipmentPackage
            {
                ShipmentPackageId = 3330000001,
                Status = "Invoiced",
                OrderDate = Now.AddHours(-2).ToUnixTimeMilliseconds(),
                CustomerFirstName = "Ali",
                CustomerLastName = "Veli",
                CustomerEmail = "pf+abc@trendyolmail.com",
                CargoProviderName = "Trendyol Express",
                CargoTrackingNumber = 7330000000001,
                ShipmentAddress = new TrendyolAddress { FullName = "Ali Veli", Phone = "05551112233", City = "İstanbul", District = "Kadıköy", FullAddress = "Moda Cd. No:1" },
                Lines = [new ShipmentLine { LineId = 555, Barcode = "BRK-1", StockCode = "SKU-1", ProductName = "Tişört", Quantity = 2, LineGrossAmount = 300m, LineTotalDiscount = 50m, VatRate = 20 }]
            }
        ])));

        var page = (await Provider.FetchOrdersAsync(Now.AddDays(-14), Now, 3, Ct)).Value!;

        await _orders.Received(1).GetPackagesAsync(
            new OrderPackagesQuery(Now.AddDays(-14), Now, null, null, null, 3, 200, "ASC"), Arg.Any<CancellationToken>());
        page.TotalPages.ShouldBe(1);
        var order = page.Orders.Single();
        order.ExternalReference.ShouldBe("3330000001");
        order.Status.ShouldBe(OrderStatus.Preparing);
        order.CustomerName.ShouldBe("Ali Veli");
        order.TrackingNumber.ShouldBe("7330000000001");
        order.PlacedAt.ShouldBe(Now.AddHours(-2).UtcDateTime);
        order.Lines.Single().ShouldBe(new MarketplaceOrderLine(555, "BRK-1", "SKU-1", "Tişört", 2, 125m, 20));
    }

    [Theory]
    [InlineData("Created", OrderStatus.Confirmed)]
    [InlineData("Picking", OrderStatus.Preparing)]
    [InlineData("Shipped", OrderStatus.Shipped)]
    [InlineData("Delivered", OrderStatus.Delivered)]
    [InlineData("UnSupplied", OrderStatus.Cancelled)]
    [InlineData("UnDelivered", null)]
    public void Package_status_maps_to_local_status(string status, OrderStatus? expected) =>
        TrendyolMarketplaceProvider.MapStatus(status).ShouldBe(expected);

    [Fact]
    public async Task Price_and_stock_are_capped_to_trendyol_limits()
    {
        _products.UpdatePriceAndInventoryAsync(default!, Ct).ReturnsForAnyArgs(
            MarketplaceResult<BatchAcceptedDto>.Success(new BatchAcceptedDto("pi-1", MarketplaceBatchOperation.PriceInventory, 1)));

        var result = await Provider.PushPriceInventoryAsync([new MarketplacePriceStock("BRK-1", 50000, 120m, 150m)], Ct);

        result.Succeeded.ShouldBeTrue();
        await _products.Received(1).UpdatePriceAndInventoryAsync(
            Arg.Is<UpdatePriceInventoryRequest>(r => r.Items.Single() == new PriceInventoryItem("BRK-1", 20000, 120m, 150m)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Picking_and_invoiced_use_package_lines()
    {
        _orders.StartPickingAsync(default, default!, Ct).ReturnsForAnyArgs(MarketplaceResult<bool>.Success(true));
        _orders.MarkInvoicedAsync(default, default!, Ct).ReturnsForAnyArgs(MarketplaceResult<bool>.Success(true));
        IReadOnlyList<MarketplaceLineQuantity> lines = [new(555, 2)];

        await Provider.UpdateOrderStatusAsync(new MarketplaceStatusCommand("3330000001", MarketplaceOrderAction.Picking, lines, null), Ct);
        await Provider.UpdateOrderStatusAsync(new MarketplaceStatusCommand("3330000001", MarketplaceOrderAction.Invoiced, lines, "GIB1"), Ct);

        await _orders.Received(1).StartPickingAsync(3330000001, Arg.Is<PackageLinesRequest>(r => r.Lines.Single() == new PackageLine(555, 2)), Arg.Any<CancellationToken>());
        await _orders.Received(1).MarkInvoicedAsync(3330000001, Arg.Is<InvoicePackageRequest>(r => r.InvoiceNumber == "GIB1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoiced_without_invoice_number_is_rejected_locally()
    {
        var result = await Provider.UpdateOrderStatusAsync(
            new MarketplaceStatusCommand("3330000001", MarketplaceOrderAction.Invoiced, [new(555, 2)], null), Ct);

        result.Error!.Kind.ShouldBe(MarketplaceErrorKind.Validation);
        await _orders.DidNotReceiveWithAnyArgs().MarkInvoicedAsync(default, default!, Ct);
    }
}
