using ECommerce.Core.Domain;
using ECommerce.Ordering;

namespace ECommerce.UnitTests.Ordering;

public class OrderTests
{
    private static readonly DateTime Now = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    public static Order NewOrder(PaymentMethod payment = PaymentMethod.BankTransfer) =>
        Order.PlaceWebOrder("EC261107ABC123", customerId: 7, "ali@example.com", "Ali Veli",
            new OrderAddress("Ali Veli", "05551112233", "İstanbul", "Kadıköy", "Moda Cd. No:1", null),
            payment,
            [new OrderLineInput(VariantId: 1, ProductId: 1, "Tişört", "Beden: M", "BRK-1", "SKU-1", UnitPrice: 150m, Quantity: 2, VatRate: 20)],
            shippingFee: 49.90m, paymentFee: payment == PaymentMethod.CashOnDelivery ? 19.90m : 0m, note: null, Now);

    [Fact]
    public void Totals_are_calculated_from_lines_and_fees()
    {
        var order = NewOrder(PaymentMethod.CashOnDelivery);

        order.Subtotal.ShouldBe(300m);
        order.Total.ShouldBe(300m + 49.90m + 19.90m);
        order.Lines.ShouldHaveSingleItem().LineTotal.ShouldBe(300m);
    }

    [Fact]
    public void Bank_transfer_waits_for_payment_and_cash_on_delivery_is_confirmed()
    {
        NewOrder(PaymentMethod.BankTransfer).Status.ShouldBe(OrderStatus.PendingPayment);
        NewOrder(PaymentMethod.CashOnDelivery).Status.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public void Order_follows_fulfilment_flow_and_records_history()
    {
        var order = NewOrder();

        order.ConfirmPayment(Now.AddHours(1));
        order.StartPreparing(Now.AddHours(2));
        order.Ship("Yurtiçi Kargo", "TRK123", Now.AddHours(3));
        order.Deliver(Now.AddDays(2));

        order.Status.ShouldBe(OrderStatus.Delivered);
        order.TrackingNumber.ShouldBe("TRK123");
        order.PaidAt.ShouldBe(Now.AddHours(1));
        order.History.Select(h => h.Status).ShouldBe(
            [OrderStatus.PendingPayment, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Shipped, OrderStatus.Delivered]);
    }

    [Fact]
    public void Invalid_transitions_are_rejected()
    {
        var order = NewOrder();

        Should.Throw<DomainException>(() => order.StartPreparing(Now));
        Should.Throw<DomainException>(() => order.Ship("Kargo", "T1", Now));
        Should.Throw<DomainException>(() => NewOrder(PaymentMethod.CashOnDelivery).ConfirmPayment(Now));
    }

    [Fact]
    public void Shipped_order_cannot_be_cancelled()
    {
        var order = NewOrder(PaymentMethod.CashOnDelivery);
        order.StartPreparing(Now);
        order.Ship("Kargo", "T1", Now);

        Should.Throw<DomainException>(() => order.Cancel("Vazgeçti", Now));
    }

    [Fact]
    public void Customer_can_cancel_only_before_preparation()
    {
        var order = NewOrder();
        order.CanCustomerCancel.ShouldBeTrue();

        order.ConfirmPayment(Now);
        order.StartPreparing(Now);
        order.CanCustomerCancel.ShouldBeFalse();

        order.Cancel("Stok sorunu", Now);
        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.CancelReason.ShouldBe("Stok sorunu");
    }

    [Fact]
    public void Order_requires_lines() =>
        Should.Throw<DomainException>(() => Order.PlaceWebOrder("EC1", 7, "a@b.com", "A", new OrderAddress("A", "1", "C", "D", "L", null),
            PaymentMethod.BankTransfer, [], 0, 0, null, Now));
}
