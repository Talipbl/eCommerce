using ECommerce.Application.Marketplaces.Trendyol;
using ECommerce.Marketplaces;
using ECommerce.Persistence;
using ECommerce.UnitTests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Trendyol.Client;
using Trendyol.Client.Products;

namespace ECommerce.UnitTests.Marketplaces.Trendyol;

public sealed class TrendyolBatchServiceTests : IAsyncDisposable
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly ITrendyolProductClient _client = Substitute.For<ITrendyolProductClient>();
    private readonly ECommerceContext _context;
    private readonly TrendyolBatchService _service;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public TrendyolBatchServiceTests()
    {
        _context = PersistenceTestContext.Create(_clock);
        _service = new TrendyolBatchService(_client, new EfRepository<MarketplaceBatchRequest>(_context), _context, _clock);
    }

    [Fact]
    public async Task Completed_batch_stores_failed_items()
    {
        await TrackAsync("batch-1", MarketplaceBatchOperation.ProductCreate);
        Returns("batch-1", new BatchRequestResult
        {
            BatchRequestId = "batch-1",
            Status = "COMPLETED",
            ItemCount = 2,
            FailedItemCount = 1,
            Items =
            [
                new BatchRequestResultItem(new BatchRequestItemReference("A"), "SUCCESS", []),
                new BatchRequestResultItem(new BatchRequestItemReference("B"), "FAILED", ["Kategori geçersiz"])
            ]
        });

        var updated = await _service.RefreshPendingAsync(Ct);

        updated.ShouldBe(1);
        var batch = await _context.MarketplaceBatchRequests.SingleAsync(Ct);
        batch.Status.ShouldBe(MarketplaceBatchStatus.Completed);
        batch.Failures.ShouldHaveSingleItem().ShouldBe(new MarketplaceBatchFailure("B", ["Kategori geçersiz"]), new MarketplaceBatchFailureComparer());
    }

    [Fact]
    public async Task In_progress_batch_stays_pending()
    {
        await TrackAsync("batch-1", MarketplaceBatchOperation.ProductCreate);
        Returns("batch-1", new BatchRequestResult { BatchRequestId = "batch-1", Status = "IN_PROGRESS", ItemCount = 1 });

        await _service.RefreshPendingAsync(Ct);

        (await _context.MarketplaceBatchRequests.SingleAsync(Ct)).Status.ShouldBe(MarketplaceBatchStatus.InProgress);
    }

    [Fact]
    public async Task Price_inventory_batch_without_status_completes_when_no_item_in_progress()
    {
        await TrackAsync("batch-1", MarketplaceBatchOperation.PriceInventory);
        Returns("batch-1", new BatchRequestResult
        {
            BatchRequestId = "batch-1",
            ItemCount = 1,
            Items = [new BatchRequestResultItem(new BatchRequestItemReference("A"), "SUCCESS", [])]
        });

        await _service.RefreshPendingAsync(Ct);

        (await _context.MarketplaceBatchRequests.SingleAsync(Ct)).Status.ShouldBe(MarketplaceBatchStatus.Completed);
    }

    [Fact]
    public async Task Batches_older_than_four_hours_expire_without_calling_trendyol()
    {
        await TrackAsync("batch-1", MarketplaceBatchOperation.ProductCreate);
        _clock.Advance(TimeSpan.FromHours(4).Add(TimeSpan.FromMinutes(1)));

        await _service.RefreshPendingAsync(Ct);

        (await _context.MarketplaceBatchRequests.SingleAsync(Ct)).Status.ShouldBe(MarketplaceBatchStatus.Expired);
        await _client.DidNotReceive().GetBatchRequestResultAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_batch_on_trendyol_expires_and_transient_error_keeps_it_pending()
    {
        await TrackAsync("gone", MarketplaceBatchOperation.ProductCreate);
        await TrackAsync("later", MarketplaceBatchOperation.ProductCreate);
        _client.GetBatchRequestResultAsync("gone", Arg.Any<CancellationToken>())
            .Returns(TrendyolResponse<BatchRequestResult>.Failure(404, new TrendyolError(null, [], "")));
        _client.GetBatchRequestResultAsync("later", Arg.Any<CancellationToken>())
            .Returns(TrendyolResponse<BatchRequestResult>.Failure(503, new TrendyolError(null, [], "")));

        await _service.RefreshPendingAsync(Ct);

        var statuses = await _context.MarketplaceBatchRequests.ToDictionaryAsync(b => b.BatchRequestId, b => b.Status, Ct);
        statuses["gone"].ShouldBe(MarketplaceBatchStatus.Expired);
        statuses["later"].ShouldBe(MarketplaceBatchStatus.Pending);
    }

    [Fact]
    public async Task List_filters_by_status_and_pages_newest_first()
    {
        await TrackAsync("old", MarketplaceBatchOperation.ProductCreate);
        _clock.Advance(TimeSpan.FromMinutes(1));
        await TrackAsync("new", MarketplaceBatchOperation.PriceInventory);

        var page = await _service.ListAsync(MarketplaceBatchStatus.Pending, page: 0, size: 1, Ct);

        page.TotalElements.ShouldBe(2);
        page.Content.ShouldHaveSingleItem().BatchRequestId.ShouldBe("new");
    }

    private async Task TrackAsync(string batchRequestId, MarketplaceBatchOperation operation)
    {
        _context.MarketplaceBatchRequests.Add(MarketplaceBatchRequest.Create("Trendyol", batchRequestId, operation, 1));
        await _context.SaveChangesAsync(Ct);
    }

    private void Returns(string batchRequestId, BatchRequestResult result) =>
        _client.GetBatchRequestResultAsync(batchRequestId, Arg.Any<CancellationToken>())
            .Returns(TrendyolResponse<BatchRequestResult>.Success(200, result));

    public ValueTask DisposeAsync() => _context.DisposeAsync();

    private sealed class MarketplaceBatchFailureComparer : IEqualityComparer<MarketplaceBatchFailure>
    {
        public bool Equals(MarketplaceBatchFailure? x, MarketplaceBatchFailure? y) =>
            x?.Barcode == y?.Barcode && (x?.Reasons ?? []).SequenceEqual(y?.Reasons ?? []);

        public int GetHashCode(MarketplaceBatchFailure obj) => obj.Barcode?.GetHashCode() ?? 0;
    }
}
