using ECommerce.Application.Catalog;
using ECommerce.Application.Common;
using ECommerce.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.UnitTests.Catalog;

public sealed class ProductAdminServiceTests : IAsyncLifetime
{
    private readonly CatalogTestDb _db = new();
    private Brand _brand = null!;
    private Category _category = null!;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _brand = await _db.AddAsync(Brand.Create("Acme", "acme"));
        _category = await _db.AddAsync(Category.Create("Tişört", "tisort", null, null, 0));
    }

    private CreateProductRequest NewRequest(string name, params VariantInput[] variants) =>
        new(name, null, "MODEL-1", _brand.Id, _category.Id, 20, "<p>Pamuk</p>",
            variants.Length > 0 ? variants : [new VariantInput("SKU-1", "BRK-1", 150m, 200m, 5, [new VariantAttributeDto("Beden", "M")])]);

    [Fact]
    public async Task Create_returns_detail_with_variants_and_generated_slug()
    {
        var result = await _db.ProductService().CreateAsync(NewRequest("Pamuklu Tişört"), Ct);

        var product = result.Value!;
        product.Slug.ShouldBe("pamuklu-tisort");
        product.IsActive.ShouldBeFalse();
        var variant = product.Variants.ShouldHaveSingleItem();
        variant.Barcode.ShouldBe("BRK-1");
        variant.Attributes.ShouldHaveSingleItem().Value.ShouldBe("M");
    }

    [Fact]
    public async Task Barcode_and_sku_must_be_unique_across_products()
    {
        var service = _db.ProductService();
        await service.CreateAsync(NewRequest("Ürün A"), Ct);

        var duplicate = await service.CreateAsync(NewRequest("Ürün B", new VariantInput("SKU-2", "BRK-1", 100m, null, 1, [])), Ct);

        duplicate.Error!.Kind.ShouldBe(ErrorKind.Conflict);
    }

    [Fact]
    public async Task Unknown_brand_is_rejected()
    {
        var request = NewRequest("Ürün") with { BrandId = 999 };

        var result = await _db.ProductService().CreateAsync(request, Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.Validation);
    }

    [Fact]
    public async Task Variant_price_stock_and_publishing_can_be_managed()
    {
        var service = _db.ProductService();
        var product = (await service.CreateAsync(NewRequest("Ürün"), Ct)).Value!;
        var variantId = product.Variants.Single().Id;

        var updated = await service.UpdateVariantAsync(product.Id, variantId, new UpdateVariantRequest(120m, 150m, 9, true, [new VariantAttributeDto("Beden", "L")]), Ct);
        var published = await service.PublishAsync(product.Id, Ct);

        updated.Value!.Variants.Single().ShouldSatisfyAllConditions(
            v => v.Price.ShouldBe(120m),
            v => v.StockQuantity.ShouldBe(9),
            v => v.Attributes.Single().Value.ShouldBe("L"));
        published.Value!.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Variants_can_be_added_and_removed()
    {
        var service = _db.ProductService();
        var product = (await service.CreateAsync(NewRequest("Ürün"), Ct)).Value!;

        var withSecond = (await service.AddVariantAsync(product.Id, new VariantInput("SKU-2", "BRK-2", 100m, null, 1, []), Ct)).Value!;
        var removed = await service.RemoveVariantAsync(product.Id, withSecond.Variants.Single(v => v.Sku == "SKU-2").Id, Ct);

        removed.Value!.Variants.ShouldHaveSingleItem().Sku.ShouldBe("SKU-1");
    }

    [Fact]
    public async Task List_filters_and_summarizes_variants()
    {
        var service = _db.ProductService();
        await service.CreateAsync(NewRequest("Mavi Tişört",
            new VariantInput("SKU-M", "BRK-M", 150m, null, 3, []),
            new VariantInput("SKU-L", "BRK-L", 120m, null, 4, [])), Ct);
        await service.CreateAsync(NewRequest("Kırmızı Elbise", new VariantInput("SKU-E", "BRK-E", 300m, null, 1, [])), Ct);

        var page = await service.ListAsync(new ProductAdminQuery("tişört", null, null, null, 0, 20), Ct);

        page.TotalElements.ShouldBe(1);
        var item = page.Content.ShouldHaveSingleItem();
        item.MinPrice.ShouldBe(120m);
        item.TotalStock.ShouldBe(7);
        item.VariantCount.ShouldBe(2);
    }

    [Fact]
    public async Task Deleted_product_disappears_with_its_variants()
    {
        var service = _db.ProductService();
        var product = (await service.CreateAsync(NewRequest("Ürün"), Ct)).Value!;

        (await service.DeleteAsync(product.Id, Ct)).Succeeded.ShouldBeTrue();

        (await service.GetAsync(product.Id, Ct)).Error!.Kind.ShouldBe(ErrorKind.NotFound);
        (await _db.Context.ProductVariants.CountAsync(Ct)).ShouldBe(0);
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
