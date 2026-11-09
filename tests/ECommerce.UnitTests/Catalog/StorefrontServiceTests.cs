using ECommerce.Application.Catalog;
using ECommerce.Application.Common;
using ECommerce.Catalog;

namespace ECommerce.UnitTests.Catalog;

public sealed class StorefrontServiceTests : IAsyncLifetime
{
    private readonly CatalogTestDb _db = new();
    private Brand _brand = null!;
    private Category _root = null!;
    private Category _child = null!;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _brand = await _db.AddAsync(Brand.Create("Acme", "acme"));
        _root = await _db.AddAsync(Category.Create("Giyim", "giyim", null, null, 0));
        _child = await _db.AddAsync(Category.Create("Tişört", "tisort", _root.Id, null, 0));
    }

    [Fact]
    public async Task Only_published_products_of_active_brand_and_category_are_listed()
    {
        await _db.AddProductAsync("Yayında", _brand, _child);
        await _db.AddProductAsync("Taslak", _brand, _child, publish: false);
        var passiveBrand = await _db.AddAsync(Brand.Create("Pasif", "pasif"));
        passiveBrand.SetActive(false);
        await _db.Context.SaveChangesAsync(Ct);
        await _db.AddProductAsync("Pasif Markalı", passiveBrand, _child);

        var page = await _db.Storefront().SearchProductsAsync(new StoreProductQuery(), Ct);

        page.Content.Select(p => p.Name).ShouldBe(["Yayında"]);
    }

    [Fact]
    public async Task Category_filter_includes_sub_categories()
    {
        await _db.AddProductAsync("Alt Kategoride", _brand, _child);

        var page = await _db.Storefront().SearchProductsAsync(new StoreProductQuery { CategorySlug = "giyim" }, Ct);

        page.Content.ShouldHaveSingleItem().Name.ShouldBe("Alt Kategoride");
    }

    [Fact]
    public async Task Price_filter_and_sorting_use_lowest_active_variant_price()
    {
        await _db.AddProductAsync("Pahalı", _brand, _child, price: 500m);
        await _db.AddProductAsync("Orta", _brand, _child, price: 200m);
        await _db.AddProductAsync("Ucuz", _brand, _child, price: 50m);

        var page = await _db.Storefront().SearchProductsAsync(new StoreProductQuery { MaxPrice = 300m, Sort = StoreProductSort.PriceAscending }, Ct);

        page.Content.Select(p => (p.Name, p.MinPrice)).ShouldBe([("Ucuz", 50m), ("Orta", 200m)]);
    }

    [Fact]
    public async Task Product_cards_expose_stock_flag_and_public_image_url()
    {
        await _db.AddProductAsync("Tükendi", _brand, _child, stock: 0);

        var card = (await _db.Storefront().SearchProductsAsync(new StoreProductQuery(), Ct)).Content.ShouldHaveSingleItem();

        card.InStock.ShouldBeFalse();
        card.ImageUrl.ShouldBe("https://cdn.test/products/tukendi/1.jpg");
        card.BrandName.ShouldBe("Acme");
    }

    [Fact]
    public async Task Product_detail_shows_category_path_and_active_variants_only()
    {
        var product = await _db.AddProductAsync("Basic Tişört", _brand, _child);
        var hidden = product.AddVariant("SKU-X", "BRK-X", 90m, null, 1, []);
        product.SetVariantActive(hidden, false);
        await _db.Context.SaveChangesAsync(Ct);

        var detail = (await _db.Storefront().GetProductAsync("basic-tisort", Ct)).Value!;

        detail.CategoryPath.Select(c => c.Slug).ShouldBe(["giyim", "tisort"]);
        detail.Variants.ShouldHaveSingleItem().Attributes.Single().Value.ShouldBe("M");
        detail.Images.ShouldHaveSingleItem().ShouldStartWith("https://cdn.test/");
    }

    [Fact]
    public async Task Unpublished_or_unknown_product_detail_is_not_found()
    {
        await _db.AddProductAsync("Taslak", _brand, _child, publish: false);

        (await _db.Storefront().GetProductAsync("taslak", Ct)).Error!.Kind.ShouldBe(ErrorKind.NotFound);
        (await _db.Storefront().GetProductAsync("yok", Ct)).Error!.Kind.ShouldBe(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Category_tree_contains_only_active_categories()
    {
        _child.SetActive(false);
        await _db.Context.SaveChangesAsync(Ct);

        var tree = await _db.Storefront().GetCategoriesAsync(Ct);

        tree.ShouldHaveSingleItem().Children.ShouldBeEmpty();
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
