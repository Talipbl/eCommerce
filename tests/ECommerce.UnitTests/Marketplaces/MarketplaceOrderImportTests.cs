using ECommerce.Application.Marketplaces;
using ECommerce.Catalog;
using ECommerce.Marketplaces;
using ECommerce.Ordering;
using ECommerce.Persistence;
using ECommerce.UnitTests.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace ECommerce.UnitTests.Marketplaces;

public sealed class MarketplaceOrderImportTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);
    private readonly CatalogTestDb _db = new();
    private readonly IMarketplaceProvider _provider = MarketplaceTestProvider.Create();
    private readonly FakeTimeProvider _clock = new(Now);
    private Product _product = null!;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private MarketplaceOrderImportService Service => new([_provider], new EfRepository<Order>(_db.Context), _db.Products, _db.Variants,
        new EfRepository<MarketplaceSyncState>(_db.Context), new EfRepository<MarketplaceSyncLog>(_db.Context), _db.Context, _clock);

    public async ValueTask InitializeAsync()
    {
        var brand = await _db.AddAsync(Brand.Create("Acme", "acme"));
        var category = await _db.AddAsync(Category.Create("Tişört", "tisort", null, null, 0));
        _product = await _db.AddProductAsync("Basic Tişört", brand, category, price: 150m, stock: 5);
    }

    private static MarketplaceOrder Package(string reference, OrderStatus? status, params MarketplaceOrderLine[] lines) => new(
        reference, status, "pf+abc@trendyolmail.com", "Ali Veli",
        new OrderAddress("Ali Veli", "05551112233", "İstanbul", "Kadıköy", "Moda Cd. No:1", null),
        lines.Length > 0 ? lines : [new MarketplaceOrderLine(555, "BRK-basic-tisort", null, "Basic Tişört", 2, 125m, 20)],
        Now.UtcDateTime.AddHours(-2), "Trendyol Express", "7330000000001");

    private void Returns(params MarketplaceOrder[] orders) =>
        _provider.FetchOrdersAsync(default, default, default, Ct).ReturnsForAnyArgs(MarketplaceResult<MarketplaceOrderPage>.Success(new MarketplaceOrderPage(orders, 1)));

    private Task<int> StockAsync() => _db.Context.ProductVariants.Select(v => v.StockQuantity).SingleAsync(Ct);

    [Fact]
    public async Task New_package_is_imported_once_and_decreases_stock()
    {
        Returns(Package("3330000001", OrderStatus.Confirmed));

        var first = await Service.ImportAsync(MarketplaceNames.Trendyol, Ct);
        var second = await Service.ImportAsync(MarketplaceNames.Trendyol, Ct);

        first.Imported.ShouldBe(1);
        second.Imported.ShouldBe(0);
        var order = await _db.Context.Orders.SingleAsync(Ct);
        order.Channel.ShouldBe(OrderChannel.Trendyol);
        order.ExternalReference.ShouldBe("3330000001");
        order.OrderNumber.ShouldBe("TY3330000001");
        order.Status.ShouldBe(OrderStatus.Confirmed);
        order.Total.ShouldBe(250m);
        order.TrackingNumber.ShouldBe("7330000000001");
        order.Lines.Single().VariantId.ShouldBe(_product.Variants.Single().Id);
        order.Lines.Single().ExternalLineId.ShouldBe(555);
        (await StockAsync()).ShouldBe(3);
    }

    [Fact]
    public async Task Unknown_barcode_is_imported_without_stock_change()
    {
        Returns(Package("3330000002", OrderStatus.Confirmed, new MarketplaceOrderLine(1, "YABANCI", null, "Bilinmeyen", 1, 100m, 20)));

        await Service.ImportAsync(MarketplaceNames.Trendyol, Ct);

        (await _db.Context.Orders.SingleAsync(Ct)).Lines.Single().VariantId.ShouldBeNull();
        (await StockAsync()).ShouldBe(5);
    }

    [Fact]
    public async Task Later_status_changes_are_mirrored()
    {
        Returns(Package("3330000003", OrderStatus.Confirmed));
        await Service.ImportAsync(MarketplaceNames.Trendyol, Ct);

        Returns(Package("3330000003", OrderStatus.Shipped));
        var result = await Service.ImportAsync(MarketplaceNames.Trendyol, Ct);

        result.Updated.ShouldBe(1);
        (await _db.Context.Orders.SingleAsync(Ct)).Status.ShouldBe(OrderStatus.Shipped);
    }

    [Fact]
    public async Task Cancellation_on_marketplace_cancels_local_order_and_restores_stock()
    {
        Returns(Package("3330000004", OrderStatus.Confirmed));
        await Service.ImportAsync(MarketplaceNames.Trendyol, Ct);
        (await StockAsync()).ShouldBe(3);

        Returns(Package("3330000004", OrderStatus.Cancelled));
        await Service.ImportAsync(MarketplaceNames.Trendyol, Ct);

        (await _db.Context.Orders.SingleAsync(Ct)).Status.ShouldBe(OrderStatus.Cancelled);
        (await StockAsync()).ShouldBe(5);
    }

    [Fact]
    public async Task Package_first_seen_as_cancelled_or_unknown_is_skipped()
    {
        Returns(Package("3330000005", OrderStatus.Cancelled), Package("3330000006", null));

        var result = await Service.ImportAsync(MarketplaceNames.Trendyol, Ct);

        result.Imported.ShouldBe(0);
        result.Skipped.ShouldBe(2);
        (await _db.Context.Orders.CountAsync(Ct)).ShouldBe(0);
    }

    [Fact]
    public async Task Stock_never_goes_negative_when_marketplace_sells_more_than_local_stock()
    {
        Returns(Package("3330000007", OrderStatus.Confirmed, new MarketplaceOrderLine(1, "BRK-basic-tisort", null, "Basic Tişört", 9, 150m, 20)));

        await Service.ImportAsync(MarketplaceNames.Trendyol, Ct);

        (await StockAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task First_run_reads_the_last_window_and_all_pages_then_moves_the_cursor()
    {
        _provider.FetchOrdersAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), 0, Arg.Any<CancellationToken>())
            .Returns(MarketplaceResult<MarketplaceOrderPage>.Success(new MarketplaceOrderPage([Package("1", OrderStatus.Confirmed)], 2)));
        _provider.FetchOrdersAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), 1, Arg.Any<CancellationToken>())
            .Returns(MarketplaceResult<MarketplaceOrderPage>.Success(new MarketplaceOrderPage([Package("2", OrderStatus.Confirmed)], 2)));

        var result = await Service.ImportAsync(MarketplaceNames.Trendyol, Ct);

        result.Imported.ShouldBe(2);
        await _provider.Received(1).FetchOrdersAsync(Now.AddDays(-14), Now, 0, Arg.Any<CancellationToken>());
        await _provider.Received(1).FetchOrdersAsync(Now.AddDays(-14), Now, 1, Arg.Any<CancellationToken>());
        (await _db.Context.MarketplaceSyncStates.SingleAsync(Ct)).Cursor.ShouldBe(Now.UtcDateTime);
        (await _db.Context.MarketplaceSyncLogs.SingleAsync(Ct)).Operation.ShouldBe(SyncOperation.OrderImport);
    }

    [Fact]
    public async Task Later_runs_continue_from_the_cursor_in_14_day_windows()
    {
        var state = MarketplaceSyncState.Create(MarketplaceNames.Trendyol, MarketplaceSyncJobs.OrderImport);
        state.AdvanceCursor(Now.UtcDateTime.AddDays(-20));
        await _db.AddAsync(state);
        Returns();

        await Service.ImportAsync(MarketplaceNames.Trendyol, Ct);

        var overlap = MarketplaceOrderImportService.Overlap;
        var start = Now.AddDays(-20) - overlap;
        await _provider.Received(1).FetchOrdersAsync(start, start.AddDays(14), 0, Arg.Any<CancellationToken>());
        await _provider.Received(1).FetchOrdersAsync(start.AddDays(14), Now, 0, Arg.Any<CancellationToken>());
        (await _db.Context.MarketplaceSyncStates.SingleAsync(Ct)).Cursor.ShouldBe(Now.UtcDateTime);
    }

    [Fact]
    public async Task Old_cursor_is_clamped_to_the_marketplace_lookback()
    {
        var state = MarketplaceSyncState.Create(MarketplaceNames.Trendyol, MarketplaceSyncJobs.OrderImport);
        state.AdvanceCursor(Now.UtcDateTime.AddDays(-90));
        await _db.AddAsync(state);
        Returns();

        await Service.ImportAsync(MarketplaceNames.Trendyol, Ct);

        var start = Now.AddDays(-28).Add(MarketplaceOrderImportService.Overlap);
        await _provider.Received(1).FetchOrdersAsync(start, start.AddDays(14), 0, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failure_keeps_the_cursor_and_rate_limit_pauses_the_job()
    {
        _provider.FetchOrdersAsync(default, default, default, Ct)
            .ReturnsForAnyArgs(MarketplaceTestProvider.Failure<MarketplaceOrderPage>(MarketplaceErrorKind.RateLimited, "Too many requests"));

        var result = await Service.ImportAsync(MarketplaceNames.Trendyol, Ct);
        var paused = await Service.ImportAsync(MarketplaceNames.Trendyol, Ct);

        result.Error.ShouldBe("Too many requests");
        paused.Error!.ShouldContain("bekleniyor");
        await _provider.ReceivedWithAnyArgs(1).FetchOrdersAsync(default, default, default, Ct);
        var state = await _db.Context.MarketplaceSyncStates.SingleAsync(Ct);
        state.Cursor.ShouldBeNull();
        state.LastError.ShouldBe("Too many requests");
        (await _db.Context.MarketplaceSyncLogs.SingleAsync(Ct)).Succeeded.ShouldBeFalse();
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
