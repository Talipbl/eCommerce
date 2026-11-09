using ECommerce.Application.Marketplaces;
using ECommerce.Catalog;
using ECommerce.Marketplaces;
using ECommerce.Persistence;
using ECommerce.UnitTests.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace ECommerce.UnitTests.Marketplaces;

public sealed class MarketplaceInventorySyncTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);
    private readonly CatalogTestDb _db = new();
    private readonly IMarketplaceProvider _provider = MarketplaceTestProvider.Create();
    private readonly FakeTimeProvider _clock = new(Now);
    private Product _listed = null!;
    private Product _notListed = null!;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private MarketplaceInventorySyncService Service => new(
        [_provider], new EfRepository<MarketplaceInventoryChange>(_db.Context), new EfRepository<MarketplaceListing>(_db.Context),
        _db.Products, _db.Variants, new EfRepository<MarketplaceSyncState>(_db.Context), new EfRepository<MarketplaceSyncLog>(_db.Context), _db.Context, _clock);

    public async ValueTask InitializeAsync()
    {
        var brand = await _db.AddAsync(Brand.Create("Acme", "acme"));
        var category = await _db.AddAsync(Category.Create("Tişört", "tisort", null, null, 0));
        _listed = await _db.AddProductAsync("Listelenen", brand, category, price: 150m, stock: 5);
        _notListed = await _db.AddProductAsync("Listelenmeyen", brand, category, price: 90m, stock: 2);

        var listing = MarketplaceListing.Create(MarketplaceNames.Trendyol, _listed.Id);
        listing.MarkSubmitted("b", DateTime.UtcNow);
        listing.ApplyBatchResult(true, [], DateTime.UtcNow);
        await _db.AddAsync(listing);
    }

    private Task<List<MarketplaceInventoryChange>> ChangesAsync() => _db.Context.MarketplaceInventoryChanges.ToListAsync(Ct);

    [Fact]
    public async Task Price_stock_and_activity_changes_are_recorded_but_name_changes_are_not()
    {
        _listed.Variants.Single().SetStock(4);
        await _db.Context.SaveChangesAsync(Ct);
        _notListed.Variants.Single().ChangePrice(95m, null);
        await _db.Context.SaveChangesAsync(Ct);
        _listed.UpdateDetails("Yeni Ad", _listed.Slug, _listed.ProductCode, _listed.BrandId, _listed.CategoryId, 20, null);
        await _db.Context.SaveChangesAsync(Ct);

        (await ChangesAsync()).Select(c => c.VariantId)
            .ShouldBe([_listed.Variants.Single().Id, _notListed.Variants.Single().Id], ignoreOrder: true);
    }

    [Fact]
    public async Task Only_variants_of_accepted_listings_are_sent_with_current_values_and_logged()
    {
        _listed.Variants.Single().SetStock(3);
        _notListed.Variants.Single().SetStock(1);
        await _db.Context.SaveChangesAsync(Ct);

        var processed = await Service.SyncPendingAsync(Ct);

        processed.ShouldBe(2);
        await _provider.Received(1).PushPriceInventoryAsync(Arg.Is<IReadOnlyList<MarketplacePriceStock>>(items =>
            items.Count == 1 && items[0] == new MarketplacePriceStock("BRK-listelenen", 3, 150m, 150m)), Arg.Any<CancellationToken>());
        (await ChangesAsync()).ShouldAllBe(c => c.Status == OutboxStatus.Succeeded);
        var log = await _db.Context.MarketplaceSyncLogs.SingleAsync(Ct);
        log.Operation.ShouldBe(SyncOperation.PriceInventory);
        log.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Unpublished_product_is_sent_with_zero_stock()
    {
        _listed.Unpublish();
        _listed.Variants.Single().SetStock(4);
        await _db.Context.SaveChangesAsync(Ct);

        await Service.SyncPendingAsync(Ct);

        await _provider.Received(1).PushPriceInventoryAsync(Arg.Is<IReadOnlyList<MarketplacePriceStock>>(items => items.Single().Quantity == 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Transient_failure_backs_off_and_retries_later()
    {
        _provider.PushPriceInventoryAsync(default!, Ct).ReturnsForAnyArgs(MarketplaceTestProvider.Failure<bool>(MarketplaceErrorKind.Unavailable));
        _listed.Variants.Single().SetStock(2);
        await _db.Context.SaveChangesAsync(Ct);

        (await Service.SyncPendingAsync(Ct)).ShouldBe(0);
        (await Service.SyncPendingAsync(Ct)).ShouldBe(0);

        await _provider.ReceivedWithAnyArgs(1).PushPriceInventoryAsync(default!, Ct);
        var change = (await ChangesAsync()).Single();
        change.Status.ShouldBe(OutboxStatus.Pending);
        change.Attempts.ShouldBe(1);

        _provider.PushPriceInventoryAsync(default!, Ct).ReturnsForAnyArgs(MarketplaceResult<bool>.Success(true));
        _clock.Advance(TimeSpan.FromMinutes(2));
        (await Service.SyncPendingAsync(Ct)).ShouldBe(1);
        (await ChangesAsync()).Single().Status.ShouldBe(OutboxStatus.Succeeded);
    }

    [Fact]
    public async Task Rate_limit_pauses_the_marketplace_without_spending_attempts()
    {
        _provider.PushPriceInventoryAsync(default!, Ct).ReturnsForAnyArgs(MarketplaceTestProvider.Failure<bool>(MarketplaceErrorKind.RateLimited));
        _listed.Variants.Single().SetStock(2);
        await _db.Context.SaveChangesAsync(Ct);

        await Service.SyncPendingAsync(Ct);
        await Service.SyncPendingAsync(Ct);

        await _provider.ReceivedWithAnyArgs(1).PushPriceInventoryAsync(default!, Ct);
        (await ChangesAsync()).Single().Attempts.ShouldBe(0);
        var state = await _db.Context.MarketplaceSyncStates.SingleAsync(Ct);
        state.Job.ShouldBe(MarketplaceSyncJobs.PriceInventory);
        state.PausedUntil.ShouldBe(Now.UtcDateTime.AddSeconds(60));
    }

    [Fact]
    public async Task Rejected_request_fails_immediately_with_the_marketplace_message()
    {
        _provider.PushPriceInventoryAsync(default!, Ct).ReturnsForAnyArgs(
            MarketplaceTestProvider.Failure<bool>(MarketplaceErrorKind.Validation, "15 dakika boyunca aynı isteği tekrarlı olarak atamazsınız!"));
        _listed.Variants.Single().SetStock(2);
        await _db.Context.SaveChangesAsync(Ct);

        await Service.SyncPendingAsync(Ct);

        var change = (await ChangesAsync()).Single();
        change.Status.ShouldBe(OutboxStatus.Failed);
        change.Error!.ShouldContain("15 dakika");
        (await _db.Context.MarketplaceSyncLogs.SingleAsync(Ct)).Succeeded.ShouldBeFalse();
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
