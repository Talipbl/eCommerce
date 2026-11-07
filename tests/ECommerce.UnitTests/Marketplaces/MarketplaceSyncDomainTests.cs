using ECommerce.Core.Domain;
using ECommerce.Marketplaces;
using ECommerce.Ordering;

namespace ECommerce.UnitTests.Marketplaces;

public class MarketplaceSyncDomainTests
{
    private static readonly DateTime Now = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Outbox_message_backs_off_on_transient_errors_and_fails_after_max_attempts()
    {
        var change = MarketplaceInventoryChange.For(7);
        change.IsDue(Now).ShouldBeTrue();

        change.MarkTransientFailure("Trendyol ulaşılamıyor", Now);
        change.Status.ShouldBe(OutboxStatus.Pending);
        change.Attempts.ShouldBe(1);
        change.IsDue(Now).ShouldBeFalse();
        change.IsDue(Now.AddMinutes(2)).ShouldBeTrue();

        for (var i = 1; i < MarketplaceOutboxMessage.MaxAttempts; i++)
            change.MarkTransientFailure("Trendyol ulaşılamıyor", Now);

        change.Status.ShouldBe(OutboxStatus.Failed);
        change.ProcessedAt.ShouldBe(Now);
        change.Error.ShouldBe("Trendyol ulaşılamıyor");
    }

    [Fact]
    public void Failed_message_can_be_requeued_but_pending_or_succeeded_cannot()
    {
        var change = MarketplaceInventoryChange.For(7);
        Should.Throw<DomainException>(() => change.Requeue());

        change.MarkFailed("Geçersiz barkod", Now);
        change.Requeue();

        change.Status.ShouldBe(OutboxStatus.Pending);
        change.Attempts.ShouldBe(0);
        change.Error.ShouldBeNull();
        change.ProcessedAt.ShouldBeNull();
        change.IsDue(Now).ShouldBeTrue();

        change.MarkSucceeded(Now);
        change.Status.ShouldBe(OutboxStatus.Succeeded);
        Should.Throw<DomainException>(() => change.Requeue());
    }

    [Fact]
    public void Sync_state_tracks_cursor_and_rate_limit_pause()
    {
        var state = MarketplaceSyncState.Create(MarketplaceNames.Trendyol, MarketplaceSyncJobs.OrderImport);
        state.Cursor.ShouldBeNull();
        state.IsPaused(Now).ShouldBeFalse();

        state.PauseUntil(Now.AddSeconds(60));
        state.IsPaused(Now).ShouldBeTrue();
        state.IsPaused(Now.AddSeconds(61)).ShouldBeFalse();

        state.AdvanceCursor(Now);
        state.AdvanceCursor(Now.AddDays(-1));
        state.Cursor.ShouldBe(Now);
    }

    [Fact]
    public void Status_update_is_created_only_for_marketplace_orders()
    {
        var web = Order.PlaceWebOrder("W1", 1, "a@b.com", "Ali", Address, PaymentMethod.CashOnDelivery, [Line()], 0, 0, null, Now);
        Should.Throw<DomainException>(() => MarketplaceOrderStatusUpdate.For(web, MarketplaceOrderAction.Picking, MarketplaceNames.Trendyol));

        var order = Imported();
        var update = MarketplaceOrderStatusUpdate.For(order, MarketplaceOrderAction.Picking, MarketplaceNames.Trendyol);
        update.ExternalReference.ShouldBe("3330000001");
        update.Action.ShouldBe(MarketplaceOrderAction.Picking);
        update.Status.ShouldBe(OutboxStatus.Pending);
    }

    [Fact]
    public void Marketplace_order_needs_an_invoice_number_to_ship_and_keeps_marketplace_cargo()
    {
        var order = Imported();
        order.StartPreparing(Now);

        Should.Throw<DomainException>(() => order.Ship(null, null, Now));
        order.Ship(null, null, Now, "GIB2026000000001");

        order.Status.ShouldBe(OrderStatus.Shipped);
        order.InvoiceNumber.ShouldBe("GIB2026000000001");
        order.CargoProvider.ShouldBe("Trendyol Express");
        order.TrackingNumber.ShouldBe("7330000000001");
        order.Lines.Single().ExternalLineId.ShouldBe(555);
    }

    [Fact]
    public void Category_mapping_resolves_attributes_from_variant_values()
    {
        var mapping = MarketplaceCategoryMapping.Create(MarketplaceNames.Trendyol, 3, 411, "Tişört");
        mapping.SetAttributes(
        [
            new CategoryAttributeMapping(47, null, null, "Pamuk", []),
            new CategoryAttributeMapping(293, "Beden", null, null, [new AttributeValueMapping("M", 2001, null), new AttributeValueMapping("L", 2002, null)])
        ]);

        mapping.ResolveProductAttributes().ShouldBe([new ListingAttribute(47, null, "Pamuk")]);
        mapping.ResolveVariantAttributes([new("beden", "l")]).ShouldBe([new ListingAttribute(293, 2002, null)]);
        mapping.ResolveVariantAttributes([new("Beden", "XXL")]).ShouldBeEmpty();
    }

    [Fact]
    public void Attribute_mapping_must_have_a_value_source()
    {
        var mapping = MarketplaceCategoryMapping.Create(MarketplaceNames.Trendyol, 3, 411, "Tişört");

        Should.Throw<DomainException>(() => mapping.SetAttributes([new CategoryAttributeMapping(47, null, null, null, [])]));
        Should.Throw<DomainException>(() => mapping.SetAttributes(
            [new CategoryAttributeMapping(47, null, null, "A", []), new CategoryAttributeMapping(47, null, null, "B", [])]));
    }

    private static readonly OrderAddress Address = new("Ali Veli", "0555", "İstanbul", "Kadıköy", "Moda", null);

    private static OrderLineInput Line(long? externalLineId = null) =>
        new(1, 1, "Tişört", null, "BRK-1", "SKU-1", 125m, 1, 20, externalLineId);

    private static Order Imported() =>
        Order.ImportMarketplaceOrder("TY3330000001", OrderChannel.Trendyol, "3330000001", "ali@example.com", "Ali Veli", Address,
            [Line(555)], Now, "Trendyol Express", "7330000000001");
}
