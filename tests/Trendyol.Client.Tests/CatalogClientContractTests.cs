using System.Text;
using Trendyol.Client.Catalog;

namespace Trendyol.Client.Tests;

public sealed class CatalogClientContractTests : IDisposable
{
    private readonly TrendyolServer _api = new();
    private ITrendyolCatalogClient Client => _api.Client<ITrendyolCatalogClient>();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Requests_carry_basic_auth_and_self_integration_user_agent()
    {
        _api.Respond("GET", "/integration/product/product-categories", 200, """{"categories":[]}""");

        await Client.GetCategoryTreeAsync(Ct);

        var headers = _api.SingleRequest().Headers!;
        var expectedAuth = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes($"{TrendyolServer.ApiKey}:{TrendyolServer.ApiSecret}"));
        headers["Authorization"].ShouldContain(expectedAuth);
        headers["User-Agent"].ShouldContain($"{TrendyolServer.SellerId} - SelfIntegration");
    }

    [Fact]
    public async Task Category_tree_is_parsed_with_leaf_information()
    {
        _api.Respond("GET", "/integration/product/product-categories", 200, """
            {"categories":[{"id":1,"name":"Giyim","parentId":null,"subCategories":[
              {"id":2,"name":"Tişört","parentId":1,"subCategories":[]}]}]}
            """);

        var result = await Client.GetCategoryTreeAsync(Ct);

        result.IsSuccess.ShouldBeTrue();
        var root = result.Data!.Categories.ShouldHaveSingleItem();
        root.IsLeaf.ShouldBeFalse();
        var leaf = root.SubCategories.ShouldHaveSingleItem();
        leaf.ParentId.ShouldBe(1);
        leaf.IsLeaf.ShouldBeTrue();
    }

    [Fact]
    public async Task Category_attributes_are_parsed()
    {
        _api.Respond("GET", "/integration/product/categories/14609/attributes", 200, """
            {"id":14609,"name":"Muay Thai Kaskı","displayName":"Muay Thai Kaskı","categoryAttributes":[
              {"allowCustom":false,"attribute":{"id":293,"name":"Beden"},"categoryId":14609,"required":true,
               "varianter":true,"slicer":false,"allowMultipleAttributeValues":false}]}
            """);

        var result = await Client.GetCategoryAttributesAsync(14609, Ct);

        var attribute = result.Data!.CategoryAttributes.ShouldHaveSingleItem();
        attribute.Attribute.Name.ShouldBe("Beden");
        attribute.Required.ShouldBeTrue();
        attribute.Varianter.ShouldBeTrue();
    }

    [Fact]
    public async Task Attribute_values_are_paged()
    {
        _api.Respond("GET", "/integration/product/categories/10/attributes/293/values", 200, """
            {"totalElements":1,"totalPages":1,"page":2,"size":100,"content":[{"attributeValueId":1001,"attributeValue":"M"}]}
            """);

        var result = await Client.GetAttributeValuesAsync(10, 293, page: 2, size: 100, Ct);

        _api.SingleRequest().Query!["page"].ShouldContain("2");
        _api.SingleRequest().Query!["size"].ShouldContain("100");
        result.Data!.Content.ShouldHaveSingleItem().AttributeValue.ShouldBe("M");
    }

    [Fact]
    public async Task Brand_search_sends_name_and_parses_array()
    {
        _api.Respond("GET", "/integration/product/brands/by-name", 200, """[{"id":40,"name":"TRENDYOLMİLLA","luxe":false}]""");

        var result = await Client.SearchBrandsAsync("TRENDYOL MİLLA", Ct);

        _api.SingleRequest().Query!["name"].ShouldContain("TRENDYOL MİLLA");
        result.Data.ShouldHaveSingleItem().Id.ShouldBe(40);
    }

    [Fact]
    public async Task Brand_list_is_parsed()
    {
        _api.Respond("GET", "/integration/product/brands", 200, """{"brands":[{"id":10,"name":"TrendyolMilla","luxe":false}]}""");

        var result = await Client.GetBrandsAsync(cancellationToken: Ct);

        result.Data!.Brands.ShouldHaveSingleItem().Name.ShouldBe("TrendyolMilla");
    }

    public void Dispose() => _api.Dispose();
}
