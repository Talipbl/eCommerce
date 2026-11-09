using Trendyol.Client.Products;

namespace Trendyol.Client.Tests;

public sealed class ProductClientContractTests : IDisposable
{
    private const string Products = $"/integration/product/sellers/{TrendyolServer.SellerId}/products";
    private const string BatchBody = """{"batchRequestId":"batch-1"}""";

    private readonly TrendyolServer _api = new();
    private ITrendyolProductClient Client => _api.Client<ITrendyolProductClient>();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Create_products_posts_v2_items_and_omits_nulls()
    {
        _api.Respond("POST", $"/integration/product/sellers/{TrendyolServer.SellerId}/v2/products", 200, BatchBody);

        var result = await Client.CreateProductsAsync(
        [
            new CreateProductItem
            {
                Barcode = "BRK-1",
                Title = "Tişört",
                ProductMainId = "MODEL-1",
                BrandId = 10,
                CategoryId = 2,
                Quantity = 5,
                Description = "<p>Pamuklu</p>",
                ListPrice = 200m,
                SalePrice = 150.5m,
                VatRate = 20,
                Images = [new TrendyolImage("https://cdn.example.com/a.jpg")],
                Attributes = [new TrendyolProductAttribute(293, AttributeValueId: 1001), new TrendyolProductAttribute(47, CustomAttributeValue: "Mavi")],
                DeliveryOption = new TrendyolDeliveryOption(2)
            }
        ], Ct);

        result.Data!.BatchRequestId.ShouldBe("batch-1");
        TrendyolServer.ShouldBeJson(_api.SingleRequest().Body, """
            {"items":[{"barcode":"BRK-1","title":"Tişört","productMainId":"MODEL-1","brandId":10,"categoryId":2,"quantity":5,
              "description":"<p>Pamuklu</p>","listPrice":200,"salePrice":150.5,"vatRate":20,
              "images":[{"url":"https://cdn.example.com/a.jpg"}],
              "attributes":[{"attributeId":293,"attributeValueId":1001},{"attributeId":47,"customAttributeValue":"Mavi"}],
              "deliveryOption":{"deliveryDuration":2}}]}
            """);
    }

    [Fact]
    public async Task Price_and_inventory_uses_inventory_path_and_partial_items()
    {
        _api.Respond("POST", $"/integration/inventory/sellers/{TrendyolServer.SellerId}/products/price-and-inventory", 200, BatchBody);

        await Client.UpdatePriceAndInventoryAsync([new PriceInventoryItem("A", Quantity: 3), new PriceInventoryItem("B", SalePrice: 10m, ListPrice: 12m)], Ct);

        TrendyolServer.ShouldBeJson(_api.SingleRequest().Body,
            """{"items":[{"barcode":"A","quantity":3},{"barcode":"B","salePrice":10,"listPrice":12}]}""");
    }

    [Fact]
    public async Task Delete_sends_barcodes_in_body()
    {
        _api.Respond("DELETE", Products, 200, BatchBody);

        await Client.DeleteProductsAsync(["A", "B"], Ct);

        TrendyolServer.ShouldBeJson(_api.SingleRequest().Body, """{"items":[{"barcode":"A"},{"barcode":"B"}]}""");
    }

    [Fact]
    public async Task Approved_content_update_uses_content_bulk_update()
    {
        _api.Respond("POST", $"{Products}/content-bulk-update", 200, BatchBody);

        await Client.UpdateApprovedContentAsync([new ApprovedContentUpdateItem { ContentId = 9510902, Description = "Yeni" }], Ct);

        TrendyolServer.ShouldBeJson(_api.SingleRequest().Body, """{"items":[{"contentId":9510902,"description":"Yeni"}]}""");
    }

    [Fact]
    public async Task Unapproved_update_uses_unapproved_bulk_update()
    {
        _api.Respond("POST", $"{Products}/unapproved-bulk-update", 200, BatchBody);

        await Client.UpdateUnapprovedProductsAsync([new UnapprovedProductUpdateItem { Barcode = "A", Title = "Yeni başlık" }], Ct);

        TrendyolServer.ShouldBeJson(_api.SingleRequest().Body, """{"items":[{"barcode":"A","title":"Yeni başlık"}]}""");
    }

    [Fact]
    public async Task Approved_products_query_uses_lowercase_status_millisecond_dates_and_parses_variants()
    {
        _api.Respond("GET", $"{Products}/approved", 200, """
            {"totalElements":1,"totalPages":1,"page":0,"size":50,"nextPageToken":null,"content":[
              {"contentId":9510902,"productMainId":"MODEL-1","brand":{"id":10,"name":"Acme"},"category":{"id":2,"name":"Tişört"},
               "title":"Tişört","images":[{"url":"https://cdn/a.jpg"}],
               "variants":[{"variantId":1,"barcode":"BRK-1","onSale":true,"locked":false,"archived":false,
                 "stock":{"quantity":5},"price":{"salePrice":150.5,"listPrice":200}}]}]}
            """);
        var start = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await Client.GetApprovedProductsAsync(new ApprovedProductQuery
        {
            Barcode = "BRK-1",
            Status = "onSale",
            StartDate = start,
            BrandIds = [10, 11],
            Page = 1,
            Size = 50
        }, Ct);

        var query = _api.SingleRequest().Query!;
        query["barcode"].ShouldContain("BRK-1");
        query["status"].ShouldContain("onSale");
        query["startDate"].ShouldContain(start.ToUnixTimeMilliseconds().ToString());
        query["brandIds"].ShouldBe(["10", "11"], ignoreOrder: true);
        query["page"].ShouldContain("1");

        var product = result.Data!.Content.ShouldHaveSingleItem();
        product.ContentId.ShouldBe(9510902);
        var variant = product.Variants.ShouldHaveSingleItem();
        variant.Stock!.Quantity.ShouldBe(5);
        variant.Price!.SalePrice.ShouldBe(150.5m);
    }

    [Fact]
    public async Task Unapproved_products_are_parsed_with_reject_reasons()
    {
        _api.Respond("GET", $"{Products}/unapproved", 200, """
            {"totalElements":1,"totalPages":1,"page":0,"size":50,"content":[
              {"barcode":"BRK-2","status":"rejected","title":"Tişört","media":[{"url":"https://cdn/b.jpg"}],
               "rejectReasonDetails":[{"rejectReason":"Görsel","rejectReasonDetail":"Görsel çözünürlüğü düşük"}]}]}
            """);

        var result = await Client.GetUnapprovedProductsAsync(new UnapprovedProductQuery { Status = "rejected" }, Ct);

        var product = result.Data!.Content.ShouldHaveSingleItem();
        product.Status.ShouldBe("rejected");
        product.RejectReasonDetails.ShouldHaveSingleItem().RejectReasonDetail.ShouldBe("Görsel çözünürlüğü düşük");
    }

    [Fact]
    public async Task Batch_result_is_parsed()
    {
        _api.Respond("GET", $"{Products}/batch-requests/batch-1", 200, """
            {"batchRequestId":"batch-1","status":"COMPLETED","creationDate":1793491200000,"lastModification":1793491260000,
             "sourceType":"API","itemCount":2,"failedItemCount":1,"batchRequestType":"ProductV2OnBoarding","items":[
               {"requestItem":{"barcode":"A"},"status":"SUCCESS","failureReasons":[]},
               {"requestItem":{"barcode":"B"},"status":"FAILED","failureReasons":["Kategori geçersiz"]}]}
            """);

        var result = await Client.GetBatchRequestResultAsync("batch-1", Ct);

        result.Data!.Status.ShouldBe("COMPLETED");
        result.Data.FailedItemCount.ShouldBe(1);
        result.Data.Items!.Single(i => i.Status == "FAILED").FailureReasons.ShouldBe(["Kategori geçersiz"]);
    }

    [Fact]
    public async Task Error_body_is_parsed_into_messages()
    {
        _api.Respond("POST", $"{Products}/content-bulk-update", 400, """{"exception":"ClientApiValidationException","errors":[{"key":"items","message":"contentId zorunludur"}]}""");

        var result = await Client.UpdateApprovedContentAsync([new ApprovedContentUpdateItem { ContentId = 0 }], Ct);

        result.IsSuccess.ShouldBeFalse();
        result.StatusCode.ShouldBe(400);
        result.Error!.Messages.ShouldBe(["contentId zorunludur"]);
    }

    [Fact]
    public async Task Non_json_error_keeps_raw_body()
    {
        _api.Respond("POST", $"{Products}/content-bulk-update", 403, null);

        var result = await Client.UpdateApprovedContentAsync([new ApprovedContentUpdateItem { ContentId = 1 }], Ct);

        result.StatusCode.ShouldBe(403);
        result.Error!.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task Unsafe_methods_are_not_retried_on_server_errors()
    {
        _api.Respond("POST", $"{Products}/content-bulk-update", 503, null);

        var result = await Client.UpdateApprovedContentAsync([new ApprovedContentUpdateItem { ContentId = 1 }], Ct);

        result.StatusCode.ShouldBe(503);
        _api.Server.LogEntries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Transport_failure_is_returned_as_unavailable_instead_of_throwing()
    {
        var client = Client;
        _api.Server.Stop();

        var result = await client.UpdateApprovedContentAsync([new ApprovedContentUpdateItem { ContentId = 1 }], Ct);

        result.IsSuccess.ShouldBeFalse();
        result.StatusCode.ShouldBe(503);
        result.Error!.Messages.ShouldNotBeEmpty();
    }

    public void Dispose() => _api.Dispose();
}
