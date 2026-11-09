using ECommerce.Application.Catalog;
using ECommerce.Application.Marketplaces.Trendyol;
using ECommerce.Application.Ordering;
using ECommerce.Catalog;
using ECommerce.Marketplaces;
using ECommerce.UnitTests.Catalog;
using ECommerce.UnitTests.Ordering;
using Trendyol.Client.Catalog;

namespace ECommerce.UnitTests.Common;

public class MappingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Product_detail_orders_images_and_builds_public_urls()
    {
        await using var db = new CatalogTestDb();
        var brand = await db.AddAsync(Brand.Create("Acme", "acme"));
        var category = await db.AddAsync(Category.Create("Tişört", "tisort", null, null, 0));
        var product = await db.AddProductAsync("Basic", brand, category);
        var second = product.AddImage("products/basic/2.jpg", "arka");
        await db.Context.SaveChangesAsync(Ct);
        product.ReorderImages([second.Id, product.Images.First(i => i.Id != second.Id).Id]);

        var dto = TestMapper.Instance.From(product).AddParameters("media", db.Media).AdaptToType<ProductDetailDto>();

        dto.Images.Select(i => i.Url).ShouldBe(["https://cdn.test/products/basic/2.jpg", "https://cdn.test/products/basic/1.jpg"]);
        dto.Variants.ShouldHaveSingleItem().Attributes.ShouldBe([new VariantAttributeDto("Beden", "M")]);
    }

    [Fact]
    public void Order_maps_nested_lines_history_and_summary_item_count()
    {
        var order = OrderTests.NewOrder();
        order.ConfirmPayment(new DateTime(2026, 11, 7, 11, 0, 0, DateTimeKind.Utc));

        var dto = TestMapper.Instance.Map<OrderDto>(order);
        var summary = TestMapper.Instance.Map<OrderSummaryDto>(order);

        dto.ShippingAddress.City.ShouldBe("İstanbul");
        dto.Lines.ShouldHaveSingleItem().LineTotal.ShouldBe(300m);
        dto.History.Select(h => h.Status).ShouldBe(order.History.OrderBy(h => h.ChangedAt).Select(h => h.Status));
        dto.BankTransfer.ShouldBeNull();
        summary.ItemCount.ShouldBe(2);
    }

    [Fact]
    public void Batch_uses_created_date_as_created_at()
    {
        var batch = MarketplaceBatchRequest.Create(MarketplaceNames.Trendyol, "batch-1", MarketplaceBatchOperation.ProductCreate, 3);

        var dto = TestMapper.Instance.Map<BatchDto>(batch);

        dto.BatchRequestId.ShouldBe("batch-1");
        dto.CreatedAt.ShouldBe(batch.CreatedDate);
        dto.ItemCount.ShouldBe(3);
    }

    [Fact]
    public void Trendyol_catalog_models_map_renamed_fields()
    {
        var attribute = new TrendyolCategoryAttribute(new TrendyolAttributeInfo(47, "Renk"), 1, Required: true, AllowCustom: false,
            Varianter: true, Slicer: false, AllowMultipleAttributeValues: true);

        TestMapper.Instance.Map<CategoryAttributeDto>(attribute)
            .ShouldBe(new CategoryAttributeDto(47, "Renk", Required: true, AllowCustom: false, Varianter: true, Slicer: false, AllowMultipleValues: true));
        TestMapper.Instance.Map<AttributeValueDto>(new TrendyolAttributeValue(2001, "M")).ShouldBe(new AttributeValueDto(2001, "M"));
        TestMapper.Instance.Map<TrendyolBrandDto>(new TrendyolBrand(10, "Acme", Luxe: false)).ShouldBe(new TrendyolBrandDto(10, "Acme"));
    }
}
