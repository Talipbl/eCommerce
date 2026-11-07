using ECommerce.Application.Common;
using ECommerce.Application.Marketplaces;
using ECommerce.Marketplaces;
using ECommerce.Persistence;
using ECommerce.UnitTests.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.UnitTests.Marketplaces;

public sealed class MarketplaceSyncAdminServiceTests : IAsyncDisposable
{
    private static readonly DateTime Now = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);
    private readonly CatalogTestDb _db = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private MarketplaceSyncAdminService Service => new(new EfRepository<MarketplaceSyncLog>(_db.Context), new EfRepository<MarketplaceSyncState>(_db.Context),
        new EfRepository<MarketplaceInventoryChange>(_db.Context), new EfRepository<MarketplaceOrderStatusUpdate>(_db.Context), _db.Context);

    [Fact]
    public async Task Logs_are_filtered_and_newest_first()
    {
        _db.Context.AddRange(
            MarketplaceSyncLog.Write(MarketplaceNames.Trendyol, SyncOperation.PriceInventory, null, true, "ok", Now),
            MarketplaceSyncLog.Write(MarketplaceNames.Trendyol, SyncOperation.OrderImport, null, false, "hata 1", Now),
            MarketplaceSyncLog.Write(MarketplaceNames.Trendyol, SyncOperation.OrderImport, null, false, "hata 2", Now));
        await _db.Context.SaveChangesAsync(Ct);

        var failed = await Service.ListLogsAsync(new SyncLogQuery(MarketplaceNames.Trendyol, SyncOperation.OrderImport, false), Ct);

        failed.TotalElements.ShouldBe(2);
        failed.Content.Select(l => l.Message).ShouldBe(["hata 2", "hata 1"]);
    }

    [Fact]
    public async Task Failed_outbox_entries_can_be_retried_one_by_one_or_all_at_once()
    {
        var failed = MarketplaceInventoryChange.For(1);
        failed.MarkFailed("Geçersiz barkod", Now);
        var other = MarketplaceInventoryChange.For(2);
        other.MarkFailed("Geçersiz barkod", Now);
        var pending = MarketplaceInventoryChange.For(3);
        _db.Context.AddRange(failed, other, pending);
        await _db.Context.SaveChangesAsync(Ct);

        (await Service.ListOutboxAsync(OutboxKind.Inventory, OutboxStatus.Failed, 0, 50, Ct)).TotalElements.ShouldBe(2);
        (await Service.RetryAsync(OutboxKind.Inventory, pending.Id, Ct)).Error!.Kind.ShouldBe(ErrorKind.Conflict);
        (await Service.RetryAsync(OutboxKind.Inventory, 999, Ct)).Error!.Kind.ShouldBe(ErrorKind.NotFound);

        var retried = await Service.RetryAsync(OutboxKind.Inventory, failed.Id, Ct);
        retried.Value!.Status.ShouldBe(OutboxStatus.Pending);
        (await Service.RetryAllFailedAsync(OutboxKind.Inventory, Ct)).ShouldBe(1);
        (await _db.Context.MarketplaceInventoryChanges.CountAsync(c => c.Status == OutboxStatus.Pending, Ct)).ShouldBe(3);
    }

    [Fact]
    public async Task States_show_cursor_and_last_error()
    {
        var state = MarketplaceSyncState.Create(MarketplaceNames.Trendyol, MarketplaceSyncJobs.OrderImport);
        state.AdvanceCursor(Now);
        state.RecordRun(Now, "Too many requests");
        await _db.AddAsync(state);

        var states = await Service.ListStatesAsync(Ct);

        states.Single().ShouldBe(new SyncStateDto(MarketplaceNames.Trendyol, MarketplaceSyncJobs.OrderImport, Now, null, Now, "Too many requests"));
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
