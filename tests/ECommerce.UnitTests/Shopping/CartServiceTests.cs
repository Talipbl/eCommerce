using ECommerce.Application.Common;
using ECommerce.Application.Shopping;
using ECommerce.Catalog;
using ECommerce.Core.Domain;
using ECommerce.Persistence;
using ECommerce.Shopping;
using ECommerce.UnitTests.Catalog;

namespace ECommerce.UnitTests.Shopping;

public sealed class CartServiceTests : IAsyncLifetime
{
    private const int CustomerId = 7;
    private readonly CatalogTestDb _db = new();
    private Product _product = null!;
    private int _variantId;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private CartService Service => new(new EfRepository<Cart>(_db.Context), _db.Products, _db.Variants, _db.Context, _db.Media);

    public async ValueTask InitializeAsync()
    {
        var brand = await _db.AddAsync(Brand.Create("Acme", "acme"));
        var category = await _db.AddAsync(Category.Create("Tişört", "tisort", null, null, 0));
        _product = await _db.AddProductAsync("Basic Tişört", brand, category, price: 150m, stock: 5);
        _variantId = _product.Variants.Single().Id;
    }

    [Fact]
    public async Task Adding_items_creates_cart_with_current_prices()
    {
        var cart = (await Service.AddItemAsync(CustomerId, new AddCartItemRequest(_variantId, 2), Ct)).Value!;

        var item = cart.Items.ShouldHaveSingleItem();
        item.ProductName.ShouldBe("Basic Tişört");
        item.UnitPrice.ShouldBe(150m);
        item.LineTotal.ShouldBe(300m);
        item.IsAvailable.ShouldBeTrue();
        item.ImageUrl.ShouldNotBeNull();
        cart.Subtotal.ShouldBe(300m);
    }

    [Fact]
    public async Task Adding_same_variant_merges_quantity_and_respects_stock()
    {
        await Service.AddItemAsync(CustomerId, new AddCartItemRequest(_variantId, 2), Ct);
        var merged = await Service.AddItemAsync(CustomerId, new AddCartItemRequest(_variantId, 3), Ct);
        var tooMany = await Service.AddItemAsync(CustomerId, new AddCartItemRequest(_variantId, 1), Ct);

        merged.Value!.Items.ShouldHaveSingleItem().Quantity.ShouldBe(5);
        tooMany.Error!.Kind.ShouldBe(ErrorKind.Validation);
    }

    [Fact]
    public async Task Unpublished_product_cannot_be_added()
    {
        _product.Unpublish();
        await _db.Context.SaveChangesAsync(Ct);

        var result = await Service.AddItemAsync(CustomerId, new AddCartItemRequest(_variantId, 1), Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.Validation);
    }

    [Fact]
    public async Task Item_becomes_unavailable_when_product_is_unpublished_or_stock_drops()
    {
        await Service.AddItemAsync(CustomerId, new AddCartItemRequest(_variantId, 3), Ct);
        _product.Variants.Single().SetStock(2);
        await _db.Context.SaveChangesAsync(Ct);

        var cart = await Service.GetAsync(CustomerId, Ct);

        cart.Items.Single().IsAvailable.ShouldBeFalse();
        cart.Items.Single().AvailableStock.ShouldBe(2);
        cart.HasUnavailableItems.ShouldBeTrue();
    }

    [Fact]
    public async Task Setting_quantity_to_zero_removes_item()
    {
        await Service.AddItemAsync(CustomerId, new AddCartItemRequest(_variantId, 1), Ct);

        var cart = (await Service.UpdateItemAsync(CustomerId, _variantId, 0, Ct)).Value!;

        cart.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Cart_line_quantity_is_limited()
    {
        var cart = Cart.Create(CustomerId);

        Should.Throw<DomainException>(() => cart.AddItem(1, Cart.MaxQuantityPerLine + 1));
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
