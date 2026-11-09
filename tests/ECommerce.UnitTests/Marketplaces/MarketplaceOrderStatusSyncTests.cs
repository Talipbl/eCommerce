using ECommerce.Application.Marketplaces;
using ECommerce.Marketplaces;
using ECommerce.Ordering;
using ECommerce.Persistence;
using ECommerce.UnitTests.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace ECommerce.UnitTests.Marketplaces;

public sealed class MarketplaceOrderStatusSyncTests : IAsyncDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);
    private readonly CatalogTestDb _db = new();
    private readonly IMarketplaceProvider _provider = MarketplaceTestProvider.Create();
    private readonly FakeTimeProvider _clock = new(Now);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private MarketplaceOrderStatusSyncService Service => new([_provider], new EfRepository<MarketplaceOrderStatusUpdate>(_db.Context),
        new EfRepository<Order>(_db.Context), new EfRepository<MarketplaceSyncState>(_db.Context), new EfRepository<MarketplaceSyncLog>(_db.Context),
        _db.Context, _clock);

    private async Task<Order> ShippedOrderAsync()
    {
        var order = Order.ImportMarketplaceOrder("TY3330000001", OrderChannel.Trendyol, "3330000001", "pf@trendyolmail.com", "Ali Veli",
            new OrderAddress("Ali Veli", "0555", "İstanbul", "Kadıköy", "Moda", null),
            [new OrderLineInput(null, null, "Tişört", null, "BRK-1", null, 150m, 2, 20, 555), new OrderLineInput(null, null, "Şapka", null, "BRK-2", null, 50m, 1, 20, 556)],
            Now.UtcDateTime, "Trendyol Express", "7330000000001");
        await _db.AddAsync(order);
        order.StartPreparing(Now.UtcDateTime);
        _db.Context.Add(MarketplaceOrderStatusUpdate.For(order, MarketplaceOrderAction.Picking, MarketplaceNames.Trendyol));
        order.Ship(null, null, Now.UtcDateTime, "GIB2026000000001");
        _db.Context.Add(MarketplaceOrderStatusUpdate.For(order, MarketplaceOrderAction.Invoiced, MarketplaceNames.Trendyol));
        await _db.Context.SaveChangesAsync(Ct);
        return order;
    }

    private Task<List<MarketplaceOrderStatusUpdate>> UpdatesAsync() => _db.Context.MarketplaceOrderStatusUpdates.OrderBy(u => u.Id).ToListAsync(Ct);

    [Fact]
    public async Task Picking_then_invoiced_are_sent_with_marketplace_line_ids()
    {
        await ShippedOrderAsync();

        (await Service.SyncPendingAsync(Ct)).ShouldBe(2);

        Received.InOrder(() =>
        {
            _provider.UpdateOrderStatusAsync(Arg.Is<MarketplaceStatusCommand>(c =>
                c.ExternalReference == "3330000001" && c.Action == MarketplaceOrderAction.Picking &&
                c.Lines.SequenceEqual(new[] { new MarketplaceLineQuantity(555, 2), new MarketplaceLineQuantity(556, 1) })), Arg.Any<CancellationToken>());
            _provider.UpdateOrderStatusAsync(Arg.Is<MarketplaceStatusCommand>(c =>
                c.Action == MarketplaceOrderAction.Invoiced && c.InvoiceNumber == "GIB2026000000001"), Arg.Any<CancellationToken>());
        });
        (await UpdatesAsync()).ShouldAllBe(u => u.Status == OutboxStatus.Succeeded);
        (await _db.Context.MarketplaceSyncLogs.CountAsync(l => l.Operation == SyncOperation.OrderStatus && l.Succeeded, Ct)).ShouldBe(2);
    }

    [Fact]
    public async Task Invoiced_waits_while_picking_is_still_pending()
    {
        await ShippedOrderAsync();
        _provider.UpdateOrderStatusAsync(default!, Ct).ReturnsForAnyArgs(MarketplaceTestProvider.Failure<bool>(MarketplaceErrorKind.Unavailable));

        await Service.SyncPendingAsync(Ct);

        await _provider.ReceivedWithAnyArgs(1).UpdateOrderStatusAsync(default!, Ct);
        var updates = await UpdatesAsync();
        updates[0].Attempts.ShouldBe(1);
        updates[1].Attempts.ShouldBe(0);
        updates.ShouldAllBe(u => u.Status == OutboxStatus.Pending);
    }

    [Fact]
    public async Task Rejected_update_fails_and_does_not_block_the_next_one()
    {
        await ShippedOrderAsync();
        _provider.UpdateOrderStatusAsync(Arg.Is<MarketplaceStatusCommand>(c => c.Action == MarketplaceOrderAction.Picking), Arg.Any<CancellationToken>())
            .Returns(MarketplaceTestProvider.Failure<bool>(MarketplaceErrorKind.Validation, "Paket statüsü uygun değil"));

        await Service.SyncPendingAsync(Ct);

        var updates = await UpdatesAsync();
        updates[0].Status.ShouldBe(OutboxStatus.Failed);
        updates[0].Error.ShouldBe("Paket statüsü uygun değil");
        updates[1].Status.ShouldBe(OutboxStatus.Succeeded);
    }

    [Fact]
    public async Task Rate_limit_pauses_status_updates()
    {
        await ShippedOrderAsync();
        _provider.UpdateOrderStatusAsync(default!, Ct).ReturnsForAnyArgs(MarketplaceTestProvider.Failure<bool>(MarketplaceErrorKind.RateLimited));

        await Service.SyncPendingAsync(Ct);
        await Service.SyncPendingAsync(Ct);

        await _provider.ReceivedWithAnyArgs(1).UpdateOrderStatusAsync(default!, Ct);
        (await UpdatesAsync()).ShouldAllBe(u => u.Status == OutboxStatus.Pending && u.Attempts == 0);
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
