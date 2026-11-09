using ECommerce.Core.Domain;
using ECommerce.Marketplaces;
using ECommerce.Ordering;

namespace ECommerce.UnitTests.Marketplaces;

public class MarketplaceListingTests
{
    private static readonly DateTime Now = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Listing_moves_from_submission_to_live()
    {
        var listing = MarketplaceListing.Create(MarketplaceNames.Trendyol, productId: 5);
        listing.Status.ShouldBe(ListingStatus.Draft);

        listing.MarkSubmitted("batch-1", Now);
        listing.Status.ShouldBe(ListingStatus.Submitted);
        listing.IsSyncable.ShouldBeFalse();

        listing.ApplyBatchResult(succeeded: true, [], Now);
        listing.Status.ShouldBe(ListingStatus.AwaitingApproval);
        listing.IsSyncable.ShouldBeTrue();

        listing.MarkLive(9510902, Now);
        listing.Status.ShouldBe(ListingStatus.Live);
        listing.ExternalContentId.ShouldBe(9510902);
    }

    [Fact]
    public void Failed_batch_keeps_reasons()
    {
        var listing = MarketplaceListing.Create(MarketplaceNames.Trendyol, 5);
        listing.MarkSubmitted("batch-1", Now);

        listing.ApplyBatchResult(succeeded: false, ["BRK-1: Kategori geçersiz", "BRK-2: Görsel bulunamadı"], Now);

        listing.Status.ShouldBe(ListingStatus.Failed);
        listing.StatusMessage.ShouldBe("BRK-1: Kategori geçersiz; BRK-2: Görsel bulunamadı");
    }

    [Fact]
    public void Live_listing_can_not_be_resubmitted_as_new_product()
    {
        var listing = MarketplaceListing.Create(MarketplaceNames.Trendyol, 5);
        listing.MarkSubmitted("b", Now);
        listing.ApplyBatchResult(true, [], Now);
        listing.MarkLive(1, Now);

        Should.Throw<DomainException>(() => listing.MarkSubmitted("b2", Now));
    }

    [Fact]
    public void Imported_marketplace_order_is_confirmed_without_fees()
    {
        var order = ImportedOrder();

        order.Channel.ShouldBe(OrderChannel.Trendyol);
        order.Status.ShouldBe(OrderStatus.Confirmed);
        order.PaymentMethod.ShouldBe(PaymentMethod.Marketplace);
        order.Total.ShouldBe(250m);
        order.CanCustomerCancel.ShouldBeFalse();
    }

    [Fact]
    public void Marketplace_status_is_mirrored_forward_only()
    {
        var order = ImportedOrder();

        order.ApplyMarketplaceStatus(OrderStatus.Shipped, "Trendyol Express", "7330000000000", Now).ShouldBeTrue();
        order.ApplyMarketplaceStatus(OrderStatus.Preparing, null, null, Now).ShouldBeFalse();
        order.ApplyMarketplaceStatus(OrderStatus.Delivered, null, null, Now).ShouldBeTrue();
        order.ApplyMarketplaceStatus(OrderStatus.Cancelled, null, null, Now).ShouldBeFalse();

        order.Status.ShouldBe(OrderStatus.Delivered);
        order.TrackingNumber.ShouldBe("7330000000000");
    }

    [Fact]
    public void Web_order_does_not_accept_marketplace_status()
    {
        var web = Ordering.OrderTests.NewOrder();

        Should.Throw<DomainException>(() => web.ApplyMarketplaceStatus(OrderStatus.Shipped, null, null, Now));
    }

    private static Order ImportedOrder() =>
        Order.ImportMarketplaceOrder("TY-10654411111-3330000001", OrderChannel.Trendyol, "3330000001", "ali@example.com", "Ali Veli",
            new OrderAddress("Ali Veli", "-", "İstanbul", "Kadıköy", "Moda Cd. No:1", null),
            [new OrderLineInput(1, 1, "Tişört", null, "BRK-1", "SKU-1", 125m, 2, 20)], Now);
}
