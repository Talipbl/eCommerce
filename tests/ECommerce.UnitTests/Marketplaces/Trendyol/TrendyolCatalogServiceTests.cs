using ECommerce.Application.Marketplaces;
using ECommerce.Application.Marketplaces.Trendyol;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Trendyol.Client;
using Trendyol.Client.Catalog;
using ECommerce.UnitTests.Common;

namespace ECommerce.UnitTests.Marketplaces.Trendyol;

public class TrendyolCatalogServiceTests
{
    private readonly ITrendyolCatalogClient _client = Substitute.For<ITrendyolCatalogClient>();
    private readonly TrendyolCatalogService _service;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public TrendyolCatalogServiceTests()
    {
        _service = new TrendyolCatalogService(_client, new MemoryCache(new MemoryCacheOptions()), TestMapper.Instance);
        _client.GetCategoryTreeAsync(Arg.Any<CancellationToken>()).Returns(TrendyolResponse<TrendyolCategoryTree>.Success(200, new TrendyolCategoryTree(
        [
            new TrendyolCategory(1, "Giyim", null,
            [
                new TrendyolCategory(2, "Tişört", 1, []),
                new TrendyolCategory(3, "Kadın", 1, [new TrendyolCategory(4, "Tişört & Atlet", 3, [])])
            ]),
            new TrendyolCategory(5, "Elektronik", null, [])
        ])));
    }

    [Fact]
    public async Task Search_returns_only_leaf_categories_with_path_ignoring_turkish_case()
    {
        var result = await _service.SearchCategoriesAsync("TİŞÖRT", 50, Ct);

        result.Value!.Select(c => (c.Id, c.Path)).ShouldBe([(2L, "Giyim > Tişört"), (4L, "Giyim > Kadın > Tişört & Atlet")]);
    }

    [Fact]
    public async Task Category_tree_is_cached()
    {
        await _service.SearchCategoriesAsync("tişört", 50, Ct);
        await _service.GetCategoryTreeAsync(Ct);

        await _client.Received(1).GetCategoryTreeAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failed_tree_request_is_not_cached()
    {
        _client.GetCategoryTreeAsync(Arg.Any<CancellationToken>()).Returns(
            TrendyolResponse<TrendyolCategoryTree>.Failure(503, new TrendyolError(null, [], "")),
            TrendyolResponse<TrendyolCategoryTree>.Success(200, new TrendyolCategoryTree([])));

        var first = await _service.GetCategoryTreeAsync(Ct);
        var second = await _service.GetCategoryTreeAsync(Ct);

        first.Error!.Kind.ShouldBe(MarketplaceErrorKind.Unavailable);
        second.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Search_requires_at_least_two_characters()
    {
        var result = await _service.SearchCategoriesAsync("t", 50, Ct);

        result.Error!.Kind.ShouldBe(MarketplaceErrorKind.Validation);
    }

    [Fact]
    public async Task Attributes_are_mapped()
    {
        _client.GetCategoryAttributesAsync(2, Arg.Any<CancellationToken>()).Returns(TrendyolResponse<TrendyolCategoryAttributes>.Success(200,
            new TrendyolCategoryAttributes(2, "Tişört", "Tişört",
                [new TrendyolCategoryAttribute(new TrendyolAttributeInfo(293, "Beden"), 2, Required: true, AllowCustom: false, Varianter: true, Slicer: false, AllowMultipleAttributeValues: false)])));

        var result = await _service.GetCategoryAttributesAsync(2, Ct);

        var attribute = result.Value.ShouldHaveSingleItem();
        attribute.Id.ShouldBe(293);
        attribute.Name.ShouldBe("Beden");
        attribute.Required.ShouldBeTrue();
        attribute.Varianter.ShouldBeTrue();
    }

    [Fact]
    public async Task Attribute_values_are_collected_from_all_pages()
    {
        _client.GetAttributeValuesAsync(2, 293, 0, 1000, Arg.Any<CancellationToken>())
            .Returns(TrendyolResponse<TrendyolAttributeValuePage>.Success(200, new TrendyolAttributeValuePage(0, 1000, 2, 2, [new TrendyolAttributeValue(1, "S")])));
        _client.GetAttributeValuesAsync(2, 293, 1, 1000, Arg.Any<CancellationToken>())
            .Returns(TrendyolResponse<TrendyolAttributeValuePage>.Success(200, new TrendyolAttributeValuePage(1, 1000, 2, 2, [new TrendyolAttributeValue(2, "M")])));

        var result = await _service.GetAttributeValuesAsync(2, 293, Ct);

        result.Value!.Select(v => v.Value).ShouldBe(["S", "M"]);
    }

    [Fact]
    public async Task Brand_search_maps_results()
    {
        _client.SearchBrandsAsync("Acme", Arg.Any<CancellationToken>())
            .Returns(TrendyolResponse<IReadOnlyList<TrendyolBrand>>.Success(200, [new TrendyolBrand(10, "Acme", false)]));

        var result = await _service.SearchBrandsAsync("Acme", Ct);

        result.Value.ShouldHaveSingleItem().ShouldBe(new TrendyolBrandDto(10, "Acme"));
    }
}
