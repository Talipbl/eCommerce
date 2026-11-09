using ECommerce.Marketplaces;

namespace ECommerce.UnitTests.Marketplaces;

public class MarketplaceBatchRequestTests
{
    private static readonly DateTime Now = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void New_batch_is_pending()
    {
        var batch = MarketplaceBatchRequest.Create("Trendyol", "batch-1", MarketplaceBatchOperation.ProductCreate, itemCount: 3);

        batch.Status.ShouldBe(MarketplaceBatchStatus.Pending);
        batch.IsPending.ShouldBeTrue();
        batch.ItemCount.ShouldBe(3);
    }

    [Fact]
    public void Completed_result_stores_failures_and_completion_time()
    {
        var batch = MarketplaceBatchRequest.Create("Trendyol", "batch-1", MarketplaceBatchOperation.ProductCreate, itemCount: 2);

        batch.ApplyResult(completed: true, itemCount: 2, failedItemCount: 1,
            [new MarketplaceBatchFailure("B", ["Kategori geçersiz"])], Now);

        batch.Status.ShouldBe(MarketplaceBatchStatus.Completed);
        batch.IsPending.ShouldBeFalse();
        batch.FailedItemCount.ShouldBe(1);
        batch.Failures.ShouldHaveSingleItem().Barcode.ShouldBe("B");
        batch.CompletedAt.ShouldBe(Now);
        batch.LastCheckedAt.ShouldBe(Now);
    }

    [Fact]
    public void In_progress_result_keeps_batch_pending()
    {
        var batch = MarketplaceBatchRequest.Create("Trendyol", "batch-1", MarketplaceBatchOperation.PriceInventory, itemCount: 1);

        batch.ApplyResult(completed: false, itemCount: 1, failedItemCount: 0, [], Now);

        batch.Status.ShouldBe(MarketplaceBatchStatus.InProgress);
        batch.IsPending.ShouldBeTrue();
        batch.CompletedAt.ShouldBeNull();
    }

    [Fact]
    public void Expired_batch_is_no_longer_pending()
    {
        var batch = MarketplaceBatchRequest.Create("Trendyol", "batch-1", MarketplaceBatchOperation.ProductDelete, itemCount: 1);

        batch.MarkExpired(Now);

        batch.Status.ShouldBe(MarketplaceBatchStatus.Expired);
        batch.IsPending.ShouldBeFalse();
    }
}
