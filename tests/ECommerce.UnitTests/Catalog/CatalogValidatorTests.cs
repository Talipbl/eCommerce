using ECommerce.Application.Catalog;

namespace ECommerce.UnitTests.Catalog;

public class CatalogValidatorTests
{
    private static CreateProductRequest Valid(params VariantInput[] variants) =>
        new("Tişört", null, "MODEL-1", 1, 2, 20, null, variants.Length > 0 ? variants : [new VariantInput("SKU-1", "BRK-1", 100m, 120m, 1, [])]);

    [Fact]
    public void Valid_product_passes() =>
        new CreateProductRequestValidator().Validate(Valid()).IsValid.ShouldBeTrue();

    [Fact]
    public void Duplicate_variant_barcodes_are_rejected() =>
        new CreateProductRequestValidator().Validate(Valid(
            new VariantInput("SKU-1", "BRK-1", 100m, null, 1, []),
            new VariantInput("SKU-2", "BRK-1", 100m, null, 1, []))).IsValid.ShouldBeFalse();

    [Theory]
    [InlineData("BRK 1", 100, null, 1)]
    [InlineData("BRK-1", 0, null, 1)]
    [InlineData("BRK-1", 100, 90, 1)]
    [InlineData("BRK-1", 100, null, -1)]
    public void Invalid_variant_is_rejected(string barcode, int price, int? listPrice, int stock) =>
        new CreateProductRequestValidator().Validate(Valid(new VariantInput("SKU-1", barcode, price, listPrice, stock, []))).IsValid.ShouldBeFalse();

    [Fact]
    public void Invalid_slug_is_rejected() =>
        new SaveCategoryRequestValidator().Validate(new SaveCategoryRequest("Giyim", "Giyim Kadın", null, null, 0, true)).IsValid.ShouldBeFalse();
}
