using ECommerce.Catalog;
using ECommerce.Core.Domain;

namespace ECommerce.UnitTests.Catalog;

public class ProductTests
{
    private static Product NewProduct() =>
        Product.Create("Pamuklu Tişört", "pamuklu-tisort", "MODEL-1", brandId: 1, categoryId: 2, vatRate: 20, description: "<p>Pamuk</p>");

    [Fact]
    public void Unsupported_vat_rate_is_rejected() =>
        Should.Throw<DomainException>(() => Product.Create("Ürün", "urun", "M", 1, 2, vatRate: 18, description: null));

    [Fact]
    public void Variant_requires_valid_price_and_stock()
    {
        var product = NewProduct();

        Should.Throw<DomainException>(() => product.AddVariant("SKU-1", "BRK-1", price: 0, listPrice: null, stockQuantity: 1, []));
        Should.Throw<DomainException>(() => product.AddVariant("SKU-1", "BRK-1", price: 100, listPrice: 90, stockQuantity: 1, []));
        Should.Throw<DomainException>(() => product.AddVariant("SKU-1", "BRK-1", price: 100, listPrice: null, stockQuantity: -1, []));
    }

    [Fact]
    public void Variant_sku_and_barcode_must_be_unique_within_product()
    {
        var product = NewProduct();
        product.AddVariant("SKU-1", "BRK-1", 100, 120, 5, [new VariantAttribute("Beden", "M")]);

        Should.Throw<DomainException>(() => product.AddVariant("SKU-1", "BRK-2", 100, null, 5, []));
        Should.Throw<DomainException>(() => product.AddVariant("SKU-2", "BRK-1", 100, null, 5, []));
    }

    [Fact]
    public void Publishing_requires_an_active_variant()
    {
        var product = NewProduct();
        Should.Throw<DomainException>(product.Publish);

        var variant = product.AddVariant("SKU-1", "BRK-1", 100, null, 0, []);
        product.Publish();
        product.IsActive.ShouldBeTrue();

        Should.Throw<DomainException>(() => product.SetVariantActive(variant, false));
    }

    [Fact]
    public void Last_variant_cannot_be_removed()
    {
        var product = NewProduct();
        var first = product.AddVariant("SKU-1", "BRK-1", 100, null, 1, []);
        var second = product.AddVariant("SKU-2", "BRK-2", 100, null, 1, []);

        product.RemoveVariant(second);

        product.Variants.ShouldHaveSingleItem().ShouldBe(first);
        Should.Throw<DomainException>(() => product.RemoveVariant(first));
    }

    [Fact]
    public void Images_are_ordered_and_limited_to_eight()
    {
        var product = NewProduct();
        for (var i = 0; i < 8; i++)
            product.AddImage($"products/1/{i}.jpg", null);

        product.Images.Select(img => img.SortOrder).ShouldBe(Enumerable.Range(0, 8));
        Should.Throw<DomainException>(() => product.AddImage("products/1/9.jpg", null));
    }

    [Fact]
    public void Removing_an_image_reorders_the_rest()
    {
        var product = NewProduct();
        product.AddImage("a.jpg", null);
        var middle = product.AddImage("b.jpg", null);
        product.AddImage("c.jpg", null);

        product.RemoveImage(middle);

        product.Images.Select(i => (i.Path, i.SortOrder)).ShouldBe([("a.jpg", 0), ("c.jpg", 1)]);
    }

    [Fact]
    public void Images_can_be_reordered_only_with_the_complete_set()
    {
        var product = NewProduct();
        var a = product.AddImage("a.jpg", null);
        var b = product.AddImage("b.jpg", null);
        a.Id = 1;
        b.Id = 2;

        product.ReorderImages([2, 1]);

        product.Images.OrderBy(i => i.SortOrder).Select(i => i.Path).ShouldBe(["b.jpg", "a.jpg"]);
        Should.Throw<DomainException>(() => product.ReorderImages([2]));
    }

    [Fact]
    public void Stock_is_decreased_for_orders_and_cannot_go_negative()
    {
        var variant = NewProduct().AddVariant("SKU-1", "BRK-1", 100, null, 3, []);

        variant.DecreaseStock(2);
        variant.StockQuantity.ShouldBe(1);
        Should.Throw<DomainException>(() => variant.DecreaseStock(2));

        variant.IncreaseStock(4);
        variant.StockQuantity.ShouldBe(5);
    }

    [Fact]
    public void Variant_price_and_stock_can_change()
    {
        var variant = NewProduct().AddVariant("SKU-1", "BRK-1", 100, null, 1, []);

        variant.ChangePrice(80, 100);
        variant.SetStock(7);

        variant.Price.ShouldBe(80);
        variant.ListPrice.ShouldBe(100);
        variant.StockQuantity.ShouldBe(7);
        Should.Throw<DomainException>(() => variant.ChangePrice(80, 70));
    }
}
