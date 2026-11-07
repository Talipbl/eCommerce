using ECommerce.Application.Common;
using ECommerce.Application.Ordering;
using ECommerce.Application.Shopping;
using ECommerce.Catalog;
using ECommerce.Core.Domain;
using ECommerce.Customers;
using ECommerce.Marketplaces;
using ECommerce.Ordering;
using ECommerce.Persistence;
using ECommerce.Shopping;
using ECommerce.UnitTests.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace ECommerce.UnitTests.Ordering;

public sealed class OrderServiceTests : IAsyncLifetime
{
    private static readonly CheckoutOptions Options = new()
    {
        ShippingFee = 49.90m,
        FreeShippingThreshold = 750m,
        CashOnDeliveryFee = 19.90m,
        BankTransfer = new BankTransferOptions { BankName = "Örnek Bank", AccountHolder = "Örnek Ltd", Iban = "TR00 0000 0000 0000 0000 0000 00" }
    };

    private readonly CatalogTestDb _db = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 7, 10, 0, 0, TimeSpan.Zero));
    private Customer _customer = null!;
    private Product _product = null!;
    private int _variantId;
    private int _addressId;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private EfRepository<Order> Orders => new(_db.Context);
    private CartService Carts => new(new EfRepository<Cart>(_db.Context), _db.Products, _db.Variants, _db.Context, _db.Media);
    private CheckoutService Checkout => new(new EfRepository<Cart>(_db.Context), new EfRepository<Customer>(_db.Context), _db.Products, _db.Variants,
        Orders, _db.Context, Microsoft.Extensions.Options.Options.Create(Options), _clock);
    private OrderManagementService Management => new(Orders, _db.Products, _db.Variants, new EfRepository<MarketplaceOrderStatusUpdate>(_db.Context), _db.Context, Microsoft.Extensions.Options.Options.Create(Options), _clock);

    public async ValueTask InitializeAsync()
    {
        var brand = await _db.AddAsync(Brand.Create("Acme", "acme"));
        var category = await _db.AddAsync(Category.Create("Tişört", "tisort", null, null, 0));
        _product = await _db.AddProductAsync("Basic Tişört", brand, category, price: 150m, stock: 5);
        _variantId = _product.Variants.Single().Id;

        _customer = Customer.Register("ali@example.com", "Ali", "Veli", null);
        _customer.AddAddress("Ev", "Ali Veli", "05551112233", "İstanbul", "Kadıköy", "Moda Cd. No:1", "34710");
        await _db.AddAsync(_customer);
        _addressId = _customer.Addresses.Single().Id;
    }

    private async Task AddToCartAsync(int quantity) =>
        (await Carts.AddItemAsync(_customer.Id, new AddCartItemRequest(_variantId, quantity), Ct)).Succeeded.ShouldBeTrue();

    [Fact]
    public async Task Bank_transfer_order_decrements_stock_clears_cart_and_shows_bank_details()
    {
        await AddToCartAsync(2);

        var order = (await Checkout.PlaceOrderAsync(_customer.Id, new CheckoutRequest(_addressId, PaymentMethod.BankTransfer, "Kapıya bırakın"), Ct)).Value!;

        order.Status.ShouldBe(OrderStatus.PendingPayment);
        order.Subtotal.ShouldBe(300m);
        order.ShippingFee.ShouldBe(49.90m);
        order.Total.ShouldBe(349.90m);
        order.OrderNumber.ShouldStartWith("EC");
        order.ShippingAddress.City.ShouldBe("İstanbul");
        order.BankTransfer!.Iban.ShouldBe(Options.BankTransfer.Iban);
        order.Lines.Single().VariantDescription.ShouldBe("Beden: M");

        (await _db.Context.ProductVariants.SingleAsync(Ct)).StockQuantity.ShouldBe(3);
        (await Carts.GetAsync(_customer.Id, Ct)).Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Cash_on_delivery_adds_fee_and_free_shipping_applies_over_threshold()
    {
        _product.Variants.Single().ChangePrice(400m, null);
        await _db.Context.SaveChangesAsync(Ct);
        await AddToCartAsync(2);

        var order = (await Checkout.PlaceOrderAsync(_customer.Id, new CheckoutRequest(_addressId, PaymentMethod.CashOnDelivery, null), Ct)).Value!;

        order.Status.ShouldBe(OrderStatus.Confirmed);
        order.ShippingFee.ShouldBe(0m);
        order.PaymentFee.ShouldBe(19.90m);
        order.Total.ShouldBe(819.90m);
        order.BankTransfer.ShouldBeNull();
    }

    [Fact]
    public async Task Checkout_fails_when_stock_dropped_after_adding_to_cart()
    {
        await AddToCartAsync(3);
        _product.Variants.Single().SetStock(1);
        await _db.Context.SaveChangesAsync(Ct);

        var result = await Checkout.PlaceOrderAsync(_customer.Id, new CheckoutRequest(_addressId, PaymentMethod.BankTransfer, null), Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.Validation);
        (await _db.Context.Orders.CountAsync(Ct)).ShouldBe(0);
        (await _db.Context.ProductVariants.SingleAsync(Ct)).StockQuantity.ShouldBe(1);
    }

    [Fact]
    public async Task Empty_cart_and_unknown_address_are_rejected()
    {
        (await Checkout.PlaceOrderAsync(_customer.Id, new CheckoutRequest(_addressId, PaymentMethod.BankTransfer, null), Ct))
            .Error!.Kind.ShouldBe(ErrorKind.Validation);

        await AddToCartAsync(1);
        (await Checkout.PlaceOrderAsync(_customer.Id, new CheckoutRequest(999, PaymentMethod.BankTransfer, null), Ct))
            .Error!.Kind.ShouldBe(ErrorKind.Validation);
    }

    [Fact]
    public async Task Preview_matches_order_totals()
    {
        await AddToCartAsync(2);

        var preview = (await Checkout.PreviewAsync(_customer.Id, PaymentMethod.CashOnDelivery, Ct)).Value!;

        preview.ShouldBe(new CheckoutPreviewDto(300m, 49.90m, 19.90m, 369.80m));
    }

    [Fact]
    public async Task Customer_cancellation_restores_stock()
    {
        await AddToCartAsync(2);
        var order = (await Checkout.PlaceOrderAsync(_customer.Id, new CheckoutRequest(_addressId, PaymentMethod.BankTransfer, null), Ct)).Value!;

        var cancelled = await Management.CancelByCustomerAsync(_customer.Id, order.OrderNumber, Ct);

        cancelled.Value!.Status.ShouldBe(OrderStatus.Cancelled);
        (await _db.Context.ProductVariants.SingleAsync(Ct)).StockQuantity.ShouldBe(5);
    }

    [Fact]
    public async Task Customer_sees_only_own_orders()
    {
        await AddToCartAsync(1);
        var order = (await Checkout.PlaceOrderAsync(_customer.Id, new CheckoutRequest(_addressId, PaymentMethod.BankTransfer, null), Ct)).Value!;

        (await Management.ListForCustomerAsync(_customer.Id, 0, 20, Ct)).Content.ShouldHaveSingleItem().OrderNumber.ShouldBe(order.OrderNumber);
        (await Management.GetForCustomerAsync(999, order.OrderNumber, Ct)).Error!.Kind.ShouldBe(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Admin_moves_order_through_fulfilment_and_filters_list()
    {
        await AddToCartAsync(1);
        var placed = (await Checkout.PlaceOrderAsync(_customer.Id, new CheckoutRequest(_addressId, PaymentMethod.BankTransfer, null), Ct)).Value!;

        await Management.ConfirmPaymentAsync(placed.Id, Ct);
        await Management.StartPreparingAsync(placed.Id, Ct);
        var shipped = (await Management.ShipAsync(placed.Id, new ShipOrderRequest("Yurtiçi Kargo", "TRK1"), Ct)).Value!;

        shipped.Status.ShouldBe(OrderStatus.Shipped);
        (await Management.ListAsync(new OrderAdminQuery(OrderStatus.Shipped, null, null, 0, 20), Ct)).TotalElements.ShouldBe(1);
        (await Management.ListAsync(new OrderAdminQuery(null, null, "ali@", 0, 20), Ct)).TotalElements.ShouldBe(1);
        await Should.ThrowAsync<DomainException>(() => Management.CancelAsync(placed.Id, "Vazgeçildi", Ct));
        (await _db.Context.MarketplaceOrderStatusUpdates.CountAsync(Ct)).ShouldBe(0);
    }

    [Fact]
    public async Task Preparing_and_shipping_a_trendyol_order_queues_picking_and_invoiced_feedback()
    {
        var order = Order.ImportMarketplaceOrder("TY3330000001", OrderChannel.Trendyol, "3330000001", "pf@trendyolmail.com", "Ali Veli",
            new OrderAddress("Ali Veli", "0555", "İstanbul", "Kadıköy", "Moda", null),
            [new OrderLineInput(_variantId, _product.Id, "Tişört", null, "BRK-1", null, 150m, 1, 20, ExternalLineId: 555)],
            _clock.GetUtcNow().UtcDateTime, "Trendyol Express", "7330000000001");
        await _db.AddAsync(order);

        await Management.StartPreparingAsync(order.Id, Ct);
        await Should.ThrowAsync<DomainException>(() => Management.ShipAsync(order.Id, new ShipOrderRequest(null, null), Ct));
        var shipped = (await Management.ShipAsync(order.Id, new ShipOrderRequest(null, null, "GIB2026000000001"), Ct)).Value!;

        shipped.TrackingNumber.ShouldBe("7330000000001");
        var updates = await _db.Context.MarketplaceOrderStatusUpdates.OrderBy(u => u.Id).ToListAsync(Ct);
        updates.Select(u => u.Action).ShouldBe([MarketplaceOrderAction.Picking, MarketplaceOrderAction.Invoiced]);
        updates.ShouldAllBe(u => u.OrderId == order.Id && u.ExternalReference == "3330000001" && u.Status == OutboxStatus.Pending);
    }

    [Fact]
    public async Task Admin_cancellation_restores_stock()
    {
        await AddToCartAsync(2);
        var placed = (await Checkout.PlaceOrderAsync(_customer.Id, new CheckoutRequest(_addressId, PaymentMethod.CashOnDelivery, null), Ct)).Value!;

        await Management.CancelAsync(placed.Id, "Müşteri aradı", Ct);

        (await _db.Context.ProductVariants.SingleAsync(Ct)).StockQuantity.ShouldBe(5);
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
