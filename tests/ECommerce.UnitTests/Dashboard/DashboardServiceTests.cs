using ECommerce.Application.Dashboard;
using ECommerce.Application.Marketplaces;
using ECommerce.Catalog;
using ECommerce.Marketplaces;
using ECommerce.Ordering;
using ECommerce.Persistence;
using ECommerce.UnitTests.Catalog;
using ECommerce.UnitTests.Common;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace ECommerce.UnitTests.Dashboard;

public sealed class DashboardServiceTests : IAsyncDisposable
{
    // İstanbul saatiyle 2026-11-09 09:00; "bugün" 2026-11-08 21:00 UTC'de başlar.
    private static readonly DateTime Now = new(2026, 11, 9, 6, 0, 0, DateTimeKind.Utc);
    private static readonly OrderAddress Address = new("Ali Veli", "5550000000", "İstanbul", "Kadıköy", "Moda Cd. 1", null);

    private readonly CatalogTestDb _db = new();
    private readonly DashboardOptions _options = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private DashboardService Service => new(
        new EfRepository<Order>(_db.Context), _db.Products, new EfRepository<MarketplaceInventoryChange>(_db.Context),
        new EfRepository<MarketplaceOrderStatusUpdate>(_db.Context), new EfRepository<MarketplaceSyncState>(_db.Context),
        Options.Create(_options), new FakeTimeProvider(Now), TestMapper.Instance);

    private static OrderLineInput Line(decimal price) => new(null, null, "Tişört", null, "BRK-1", null, price, 1, 20);

    private static Order Web(string number, decimal total, PaymentMethod payment, DateTime placedAt) =>
        Order.PlaceWebOrder(number, 1, "ali@example.com", "Ali Veli", Address, payment, [Line(total)], 0m, 0m, null, placedAt);

    private static Order Trendyol(string number, decimal total, DateTime placedAt) =>
        Order.ImportMarketplaceOrder(number, OrderChannel.Trendyol, $"TY-{number}", "ali@example.com", "Ali Veli", Address, [Line(total)], placedAt);

    private async Task AddAsync(params object[] entities)
    {
        _db.Context.AddRange(entities);
        await _db.Context.SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task Orders_are_counted_per_status_including_empty_ones()
    {
        var shipped = Web("W3", 100m, PaymentMethod.CashOnDelivery, Now);
        shipped.StartPreparing(Now);
        shipped.Ship("Yurtiçi", "123", Now);
        await AddAsync(
            Web("W1", 100m, PaymentMethod.BankTransfer, Now),
            Web("W2", 100m, PaymentMethod.BankTransfer, Now),
            Web("W4", 100m, PaymentMethod.CashOnDelivery, Now),
            shipped);

        var summary = await Service.GetSummaryAsync(Ct);

        summary.OrdersByStatus.ShouldBe([
            new OrderStatusCountDto(OrderStatus.PendingPayment, 2),
            new OrderStatusCountDto(OrderStatus.Confirmed, 1),
            new OrderStatusCountDto(OrderStatus.Preparing, 0),
            new OrderStatusCountDto(OrderStatus.Shipped, 1),
            new OrderStatusCountDto(OrderStatus.Delivered, 0),
            new OrderStatusCountDto(OrderStatus.Cancelled, 0)
        ]);
        summary.PendingBankTransfers.ShouldBe(2);
    }

    [Fact]
    public async Task Revenue_uses_istanbul_days_per_channel_and_skips_cancelled_orders()
    {
        var cancelled = Web("W9", 1000m, PaymentMethod.CashOnDelivery, Now);
        cancelled.Cancel("Müşteri vazgeçti.", Now);
        await AddAsync(
            Web("W1", 100m, PaymentMethod.CashOnDelivery, new DateTime(2026, 11, 8, 22, 0, 0, DateTimeKind.Utc)),  // bugün 01:00
            Trendyol("T1", 250m, new DateTime(2026, 11, 9, 5, 0, 0, DateTimeKind.Utc)),                             // bugün 08:00
            Web("W2", 40m, PaymentMethod.BankTransfer, new DateTime(2026, 11, 8, 20, 0, 0, DateTimeKind.Utc)),     // dün 23:00
            Trendyol("T2", 60m, new DateTime(2026, 11, 2, 21, 30, 0, DateTimeKind.Utc)),                           // 7 gün önce 00:30
            Web("W3", 999m, PaymentMethod.CashOnDelivery, new DateTime(2026, 11, 2, 20, 0, 0, DateTimeKind.Utc)),  // 8 gün önce 23:00
            cancelled);

        var summary = await Service.GetSummaryAsync(Ct);

        summary.Today.From.ShouldBe(new DateTime(2026, 11, 8, 21, 0, 0, DateTimeKind.Utc));
        summary.Today.Total.ShouldBe(350m);
        summary.Today.OrderCount.ShouldBe(2);
        summary.Today.ByChannel.ShouldBe([new ChannelRevenueDto(OrderChannel.Web, 100m, 1), new ChannelRevenueDto(OrderChannel.Trendyol, 250m, 1)]);
        summary.Last7Days.From.ShouldBe(new DateTime(2026, 11, 2, 21, 0, 0, DateTimeKind.Utc));
        summary.Last7Days.Total.ShouldBe(450m);
        summary.Last7Days.ByChannel.ShouldBe([new ChannelRevenueDto(OrderChannel.Web, 140m, 2), new ChannelRevenueDto(OrderChannel.Trendyol, 310m, 2)]);
    }

    [Fact]
    public async Task Low_stock_lists_active_variants_of_active_products_at_or_below_threshold()
    {
        _options.LowStockThreshold = 3;
        var brand = await _db.AddAsync(Brand.Create("Acme", "acme"));
        var category = await _db.AddAsync(Category.Create("Tişört", "tisort", null, null, 0));
        var product = await _db.AddProductAsync("Basic Tişört", brand, category, stock: 3);
        product.AddVariant("SKU-L", "BRK-L", 100m, null, 0, []);
        product.AddVariant("SKU-XL", "BRK-XL", 100m, null, 4, []);
        product.SetVariantActive(product.AddVariant("SKU-XS", "BRK-XS", 100m, null, 0, []), false);
        await _db.Context.SaveChangesAsync(Ct);
        await _db.AddProductAsync("Taslak Ürün", brand, category, stock: 0, publish: false);

        var lowStock = (await Service.GetSummaryAsync(Ct)).LowStock;

        lowStock.Threshold.ShouldBe(3);
        lowStock.TotalCount.ShouldBe(2);
        lowStock.Variants.Select(v => (v.ProductName, v.Sku, v.StockQuantity)).ShouldBe([("Basic Tişört", "SKU-L", 0), ("Basic Tişört", "SKU-basic-tisort", 3)]);
    }

    [Fact]
    public async Task Failed_outbox_entries_and_sync_jobs_with_errors_or_pauses_are_reported()
    {
        var failed = MarketplaceInventoryChange.For(1);
        failed.MarkFailed("Geçersiz barkod", Now);
        var order = Trendyol("T1", 100m, Now);
        await AddAsync(failed, MarketplaceInventoryChange.For(2), order);
        var statusFailed = MarketplaceOrderStatusUpdate.For(order, MarketplaceOrderAction.Picking, MarketplaceNames.Trendyol);
        statusFailed.MarkFailed("Paket bulunamadı", Now);
        var healthy = MarketplaceSyncState.Create(MarketplaceNames.Trendyol, MarketplaceSyncJobs.OrderImport);
        healthy.RecordRun(Now, null);
        var errored = MarketplaceSyncState.Create(MarketplaceNames.Trendyol, MarketplaceSyncJobs.PriceInventory);
        errored.RecordRun(Now, "Too many requests");
        var paused = MarketplaceSyncState.Create(MarketplaceNames.Trendyol, MarketplaceSyncJobs.OrderStatus);
        paused.PauseUntil(Now.AddMinutes(5));
        await AddAsync(statusFailed, healthy, errored, paused);

        var summary = await Service.GetSummaryAsync(Ct);

        summary.FailedOutbox.ShouldBe(new FailedOutboxDto(1, 1));
        summary.SyncAlerts.Select(s => s.Job).ShouldBe([MarketplaceSyncJobs.OrderStatus, MarketplaceSyncJobs.PriceInventory]);
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
