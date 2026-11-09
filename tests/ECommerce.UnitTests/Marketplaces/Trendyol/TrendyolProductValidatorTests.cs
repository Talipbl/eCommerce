using ECommerce.Application.Marketplaces.Trendyol;
using Trendyol.Client.Products;

namespace ECommerce.UnitTests.Marketplaces.Trendyol;

public class TrendyolProductValidatorTests
{
    private readonly CreateTrendyolProductsRequestValidator _create = new();
    private readonly UpdatePriceInventoryRequestValidator _priceInventory = new();

    [Fact]
    public void Valid_product_passes() =>
        _create.Validate(new CreateTrendyolProductsRequest([TestProducts.Valid("BRK-1.a_b")])).IsValid.ShouldBeTrue();

    [Fact]
    public void List_price_cannot_be_lower_than_sale_price() =>
        ShouldFail(TestProducts.Valid("A") with { ListPrice = 100m, SalePrice = 150m });

    [Theory]
    [InlineData("BRK 1")]
    [InlineData("BRK/1")]
    public void Barcode_allows_only_dot_dash_underscore(string barcode) =>
        ShouldFail(TestProducts.Valid(barcode));

    [Fact]
    public void Images_must_be_https_and_at_most_eight()
    {
        ShouldFail(TestProducts.Valid("A") with { Images = [new TrendyolImage("http://cdn.example.com/a.jpg")] });
        ShouldFail(TestProducts.Valid("A") with { Images = Enumerable.Range(0, 9).Select(i => new TrendyolImage($"https://cdn/{i}.jpg")).ToList() });
    }

    [Fact]
    public void Vat_rate_must_be_supported() =>
        ShouldFail(TestProducts.Valid("A") with { VatRate = 18 });

    [Fact]
    public void Barcodes_must_be_unique_in_request() =>
        _create.Validate(new CreateTrendyolProductsRequest([TestProducts.Valid("A"), TestProducts.Valid("A")])).IsValid.ShouldBeFalse();

    [Fact]
    public void Price_inventory_item_needs_a_change_and_valid_ranges()
    {
        _priceInventory.Validate(new UpdatePriceInventoryRequest([new PriceInventoryItem("A")])).IsValid.ShouldBeFalse();
        _priceInventory.Validate(new UpdatePriceInventoryRequest([new PriceInventoryItem("A", Quantity: 20001)])).IsValid.ShouldBeFalse();
        _priceInventory.Validate(new UpdatePriceInventoryRequest([new PriceInventoryItem("A", SalePrice: 20m, ListPrice: 10m)])).IsValid.ShouldBeFalse();
        _priceInventory.Validate(new UpdatePriceInventoryRequest([new PriceInventoryItem("A", Quantity: 0)])).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Content_update_needs_at_least_one_field() =>
        new UpdateTrendyolContentRequestValidator()
            .Validate(new UpdateTrendyolContentRequest([new ApprovedContentUpdateItem { ContentId = 1 }]))
            .IsValid.ShouldBeFalse();

    private void ShouldFail(CreateProductItem item) =>
        _create.Validate(new CreateTrendyolProductsRequest([item])).IsValid.ShouldBeFalse();
}
