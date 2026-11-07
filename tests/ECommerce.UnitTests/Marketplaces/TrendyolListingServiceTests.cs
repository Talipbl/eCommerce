using ECommerce.Application.Catalog;
using ECommerce.Application.Common;
using ECommerce.Application.Marketplaces;
using ECommerce.Application.Marketplaces.Trendyol;
using ECommerce.Catalog;
using ECommerce.Marketplaces;
using ECommerce.Persistence;
using ECommerce.UnitTests.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Trendyol.Client.Products;

namespace ECommerce.UnitTests.Marketplaces;

public sealed class TrendyolListingServiceTests : IAsyncLifetime
{
    private readonly CatalogTestDb _db = new();
    private readonly ITrendyolProductService _products = Substitute.For<ITrendyolProductService>();
    private readonly ITrendyolCatalogService _catalog = Substitute.For<ITrendyolCatalogService>();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 7, 12, 0, 0, TimeSpan.Zero));
    private Product _product = null!;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private TrendyolListingService Service(IMediaUrlBuilder? media = null) => new(
        new EfRepository<MarketplaceListing>(_db.Context), new EfRepository<MarketplaceCategoryMapping>(_db.Context),
        new EfRepository<MarketplaceBrandMapping>(_db.Context), new EfRepository<MarketplaceBatchRequest>(_db.Context),
        _db.Products, _db.Categories, _db.Brands, new EfRepository<MarketplaceSyncLog>(_db.Context), _db.Context, _products, _catalog, media ?? _db.Media, _clock);

    public async ValueTask InitializeAsync()
    {
        var brand = await _db.AddAsync(Brand.Create("Acme", "acme"));
        var category = await _db.AddAsync(Category.Create("Tişört", "tisort", null, null, 0));
        _product = await _db.AddProductAsync("Basic Tişört", brand, category, price: 150m, stock: 5);
        _product.Variants.Single().ChangePrice(150m, 200m);
        _product.AddVariant("SKU-L", "BRK-L", 160m, null, 0, [new VariantAttribute("Beden", "L")]);
        var inactive = _product.AddVariant("SKU-X", "BRK-X", 160m, null, 1, []);
        _product.SetVariantActive(inactive, false);
        await _db.Context.SaveChangesAsync(Ct);

        _catalog.GetCategoryTreeAsync(Arg.Any<CancellationToken>()).Returns(MarketplaceResult<IReadOnlyList<CategoryNodeDto>>.Success(
            [new CategoryNodeDto(1, "Giyim", null, false, [new CategoryNodeDto(411, "Tişört", 1, true, [])])]));
        _products.CreateAsync(Arg.Any<CreateTrendyolProductsRequest>(), Arg.Any<CancellationToken>())
            .Returns(MarketplaceResult<BatchAcceptedDto>.Success(new BatchAcceptedDto("batch-1", MarketplaceBatchOperation.ProductCreate, 2)));
    }

    private async Task MapAsync()
    {
        (await Service().MapCategoryAsync(_product.CategoryId, new MapCategoryRequest(411, "Tişört"), Ct)).Succeeded.ShouldBeTrue();
        (await Service().MapBrandAsync(_product.BrandId, new MapBrandRequest(10, "Acme"), Ct)).Succeeded.ShouldBeTrue();
    }

    private PublishListingRequest Request() => new(
        [new ListingAttribute(47, null, "Mavi")],
        _product.Variants.Where(v => v.IsActive).Select(v => new VariantListingAttributes(v.Id, [new ListingAttribute(293, v.Sku == "SKU-L" ? 2002 : 2001, null)])).ToList(),
        DeliveryDuration: 2);

    [Fact]
    public async Task Publishing_requires_category_and_brand_mappings()
    {
        var result = await Service().PublishAsync(_product.Id, Request(), Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.Validation);
        await _products.DidNotReceiveWithAnyArgs().CreateAsync(default!, Ct);
    }

    [Fact]
    public async Task Category_must_map_to_a_leaf_trendyol_category()
    {
        var result = await Service().MapCategoryAsync(_product.CategoryId, new MapCategoryRequest(1, "Giyim"), Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.Validation);
    }

    [Fact]
    public async Task Publish_sends_active_variants_with_mapped_ids_and_tracks_listing()
    {
        await MapAsync();

        var result = await Service().PublishAsync(_product.Id, Request(), Ct);

        result.Value!.Status.ShouldBe(ListingStatus.Submitted);
        result.Value.LastBatchRequestId.ShouldBe("batch-1");
        await _products.Received(1).CreateAsync(Arg.Is<CreateTrendyolProductsRequest>(r =>
            r.Items.Count == 2 &&
            r.Items.All(i => i.BrandId == 10 && i.CategoryId == 411 && i.ProductMainId == _product.ProductCode && i.VatRate == 20) &&
            r.Items.Single(i => i.Barcode == "BRK-basic-tisort").ListPrice == 200m &&
            r.Items.Single(i => i.Barcode == "BRK-L").ListPrice == 160m &&
            r.Items.All(i => i.Images.All(img => img.Url.StartsWith("https://"))) &&
            r.Items.Single(i => i.Barcode == "BRK-L").Attributes.Any(a => a.AttributeId == 293 && a.AttributeValueId == 2002) &&
            r.Items.All(i => i.Attributes.Any(a => a.AttributeId == 47 && a.CustomAttributeValue == "Mavi")) &&
            r.Items.All(i => i.DeliveryOption!.DeliveryDuration == 2)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_requires_public_https_image_urls()
    {
        await MapAsync();

        var result = await Service(new HttpMediaUrlBuilder()).PublishAsync(_product.Id, Request(), Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.Validation);
        result.Error.Message.ShouldContain("https");
    }

    [Fact]
    public async Task Trendyol_rejection_is_returned_and_listing_stays_draft()
    {
        await MapAsync();
        _products.CreateAsync(Arg.Any<CreateTrendyolProductsRequest>(), Arg.Any<CancellationToken>())
            .Returns(MarketplaceResult<BatchAcceptedDto>.Failure(new MarketplaceError(MarketplaceErrorKind.Validation, 400, ["Kategori özelliği eksik"])));

        var result = await Service().PublishAsync(_product.Id, Request(), Ct);

        result.Error!.Message.ShouldContain("Kategori özelliği eksik");
        (await _db.Context.MarketplaceListings.SingleOrDefaultAsync(Ct))?.Status.ShouldBe(ListingStatus.Draft);
    }

    [Fact]
    public async Task Refresh_applies_completed_batch_results()
    {
        await MapAsync();
        await Service().PublishAsync(_product.Id, Request(), Ct);
        var batch = MarketplaceBatchRequest.Create(MarketplaceNames.Trendyol, "batch-1", MarketplaceBatchOperation.ProductCreate, 2);
        batch.ApplyResult(true, 2, 1, [new MarketplaceBatchFailure("BRK-L", ["Görsel bulunamadı"])], DateTime.UtcNow);
        await _db.AddAsync(batch);

        await Service().RefreshStatusesAsync(Ct);

        var listing = await _db.Context.MarketplaceListings.SingleAsync(Ct);
        listing.Status.ShouldBe(ListingStatus.Failed);
        listing.StatusMessage.ShouldBe("BRK-L: Görsel bulunamadı");
    }

    [Fact]
    public async Task Refresh_marks_approved_listing_live_and_rejected_listing_rejected()
    {
        await MapAsync();
        await Service().PublishAsync(_product.Id, Request(), Ct);
        var batch = MarketplaceBatchRequest.Create(MarketplaceNames.Trendyol, "batch-1", MarketplaceBatchOperation.ProductCreate, 2);
        batch.ApplyResult(true, 2, 0, [], DateTime.UtcNow);
        await _db.AddAsync(batch);
        await Service().RefreshStatusesAsync(Ct);
        (await _db.Context.MarketplaceListings.SingleAsync(Ct)).Status.ShouldBe(ListingStatus.AwaitingApproval);

        _products.GetApprovedAsync(Arg.Any<ApprovedProductQuery>(), Arg.Any<CancellationToken>())
            .Returns(MarketplaceResult<ApprovedProductPage>.Success(new ApprovedProductPage(0, 50, 1, 1, null, [new ApprovedProduct { ContentId = 9510902 }])));
        await Service().RefreshStatusesAsync(Ct);

        var listing = await _db.Context.MarketplaceListings.SingleAsync(Ct);
        listing.Status.ShouldBe(ListingStatus.Live);
        listing.ExternalContentId.ShouldBe(9510902);
    }

    [Fact]
    public async Task Refresh_reports_rejection_reasons()
    {
        await MapAsync();
        await Service().PublishAsync(_product.Id, Request(), Ct);
        var batch = MarketplaceBatchRequest.Create(MarketplaceNames.Trendyol, "batch-1", MarketplaceBatchOperation.ProductCreate, 2);
        batch.ApplyResult(true, 2, 0, [], DateTime.UtcNow);
        await _db.AddAsync(batch);
        _products.GetApprovedAsync(Arg.Any<ApprovedProductQuery>(), Arg.Any<CancellationToken>())
            .Returns(MarketplaceResult<ApprovedProductPage>.Success(new ApprovedProductPage(0, 50, 0, 0, null, [])));
        _products.GetUnapprovedAsync(Arg.Any<UnapprovedProductQuery>(), Arg.Any<CancellationToken>())
            .Returns(MarketplaceResult<UnapprovedProductPage>.Success(new UnapprovedProductPage(0, 50, 1, 1, null,
                [new UnapprovedProduct { Barcode = "BRK-L", Status = "rejected", RejectReasonDetails = [new TrendyolRejectReason("Görsel", "Çözünürlük düşük")] }])));

        await Service().RefreshStatusesAsync(Ct);
        await Service().RefreshStatusesAsync(Ct);

        var listing = await _db.Context.MarketplaceListings.SingleAsync(Ct);
        listing.Status.ShouldBe(ListingStatus.Rejected);
        listing.StatusMessage!.ShouldContain("Çözünürlük düşük");
    }

    private void StubAttributes()
    {
        _catalog.GetCategoryAttributesAsync(411, Arg.Any<CancellationToken>()).Returns(MarketplaceResult<IReadOnlyList<CategoryAttributeDto>>.Success(
        [
            new CategoryAttributeDto(47, "Renk", Required: true, AllowCustom: true, Varianter: false, Slicer: true, AllowMultipleValues: false),
            new CategoryAttributeDto(293, "Beden", Required: true, AllowCustom: false, Varianter: true, Slicer: false, AllowMultipleValues: false),
            new CategoryAttributeDto(14, "Materyal", Required: false, AllowCustom: false, Varianter: false, Slicer: false, AllowMultipleValues: false)
        ]));
        _catalog.GetAttributeValuesAsync(411, 293, Arg.Any<CancellationToken>()).Returns(MarketplaceResult<IReadOnlyList<AttributeValueDto>>.Success(
            [new AttributeValueDto(2001, "M"), new AttributeValueDto(2002, "L")]));
        _catalog.GetAttributeValuesAsync(411, 14, Arg.Any<CancellationToken>()).Returns(MarketplaceResult<IReadOnlyList<AttributeValueDto>>.Success(
            [new AttributeValueDto(901, "Pamuk")]));
    }

    private static readonly CategoryAttributeMapping[] AttributeMappings =
    [
        new(47, null, null, "Mavi", []),
        new(293, "Beden", null, null, [new AttributeValueMapping("M", 2001, null), new AttributeValueMapping("L", 2002, null)])
    ];

    [Fact]
    public async Task Attribute_mapping_is_validated_against_the_trendyol_category()
    {
        await MapAsync();
        StubAttributes();

        (await Service().SetCategoryAttributesAsync(_product.CategoryId, new SetCategoryAttributesRequest([new(999, null, null, "x", [])]), Ct))
            .Error!.Message.ShouldContain("999");
        (await Service().SetCategoryAttributesAsync(_product.CategoryId, new SetCategoryAttributesRequest([new(293, "Beden", null, null, [new("M", 7777, null)])]), Ct))
            .Error!.Message.ShouldContain("7777");
        (await Service().SetCategoryAttributesAsync(_product.CategoryId, new SetCategoryAttributesRequest([new(14, null, null, "Keten", [])]), Ct))
            .Error!.Message.ShouldContain("serbest");

        var saved = await Service().SetCategoryAttributesAsync(_product.CategoryId, new SetCategoryAttributesRequest(AttributeMappings), Ct);
        saved.Value!.Attributes.Count.ShouldBe(2);
        (await Service().ListCategoryMappingsAsync(Ct)).Single().Attributes.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Attribute_mapping_needs_a_category_mapping()
    {
        var result = await Service().SetCategoryAttributesAsync(_product.CategoryId, new SetCategoryAttributesRequest(AttributeMappings), Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Publish_without_request_attributes_uses_the_category_mapping_and_logs()
    {
        await MapAsync();
        StubAttributes();
        await Service().SetCategoryAttributesAsync(_product.CategoryId, new SetCategoryAttributesRequest(AttributeMappings), Ct);

        var result = await Service().PublishAsync(_product.Id, new PublishListingRequest(), Ct);

        result.Succeeded.ShouldBeTrue();
        await _products.Received(1).CreateAsync(Arg.Is<CreateTrendyolProductsRequest>(r =>
            r.Items.Single(i => i.Barcode == "BRK-L").Attributes.Any(a => a.AttributeId == 293 && a.AttributeValueId == 2002) &&
            r.Items.Single(i => i.Barcode == "BRK-basic-tisort").Attributes.Any(a => a.AttributeId == 293 && a.AttributeValueId == 2001) &&
            r.Items.All(i => i.Attributes.Any(a => a.AttributeId == 47 && a.CustomAttributeValue == "Mavi"))), Arg.Any<CancellationToken>());
        var log = await _db.Context.MarketplaceSyncLogs.SingleAsync(Ct);
        log.Operation.ShouldBe(SyncOperation.Publish);
        log.Reference.ShouldBe(_product.Id.ToString());
        log.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Request_attributes_override_mapping_and_missing_required_attributes_are_reported()
    {
        await MapAsync();
        StubAttributes();
        await Service().SetCategoryAttributesAsync(_product.CategoryId, new SetCategoryAttributesRequest([new(47, null, null, "Mavi", [])]), Ct);

        var missing = await Service().PublishAsync(_product.Id, new PublishListingRequest(), Ct);
        missing.Error!.Message.ShouldContain("Beden");
        await _products.DidNotReceiveWithAnyArgs().CreateAsync(default!, Ct);

        var result = await Service().PublishAsync(_product.Id, new PublishListingRequest([new ListingAttribute(47, null, "Kırmızı")],
            _product.Variants.Where(v => v.IsActive).Select(v => new VariantListingAttributes(v.Id, [new ListingAttribute(293, 2001, null)])).ToList()), Ct);

        result.Succeeded.ShouldBeTrue();
        await _products.Received(1).CreateAsync(Arg.Is<CreateTrendyolProductsRequest>(r =>
            r.Items.All(i => i.Attributes.Count(a => a.AttributeId == 47) == 1 && i.Attributes.Any(a => a.CustomAttributeValue == "Kırmızı"))), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejected_publish_is_logged_as_failure()
    {
        await MapAsync();
        _products.CreateAsync(Arg.Any<CreateTrendyolProductsRequest>(), Arg.Any<CancellationToken>())
            .Returns(MarketplaceResult<BatchAcceptedDto>.Failure(new MarketplaceError(MarketplaceErrorKind.Validation, 400, ["Kategori özelliği eksik"])));

        await Service().PublishAsync(_product.Id, Request(), Ct);

        var log = await _db.Context.MarketplaceSyncLogs.SingleAsync(Ct);
        log.Succeeded.ShouldBeFalse();
        log.Message!.ShouldContain("Kategori özelliği eksik");
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private sealed class HttpMediaUrlBuilder : IMediaUrlBuilder
    {
        public string ToPublicUrl(string path) => $"http://localhost/media/{path}";
    }
}
