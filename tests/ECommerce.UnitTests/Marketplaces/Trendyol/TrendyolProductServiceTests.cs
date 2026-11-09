using ECommerce.Application.Marketplaces;
using ECommerce.Application.Marketplaces.Trendyol;
using ECommerce.Marketplaces;
using ECommerce.Persistence;
using ECommerce.UnitTests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Trendyol.Client;
using Trendyol.Client.Products;

namespace ECommerce.UnitTests.Marketplaces.Trendyol;

public sealed class TrendyolProductServiceTests : IAsyncDisposable
{
    private readonly ITrendyolProductClient _client = Substitute.For<ITrendyolProductClient>();
    private readonly ECommerceContext _context = PersistenceTestContext.Create(new FakeTimeProvider(DateTimeOffset.UtcNow));
    private readonly TrendyolProductService _service;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public TrendyolProductServiceTests() =>
        _service = new TrendyolProductService(_client, new TrendyolBatchTracker(new EfRepository<MarketplaceBatchRequest>(_context), _context));

    [Fact]
    public async Task Create_tracks_returned_batch()
    {
        _client.CreateProductsAsync(Arg.Any<IReadOnlyList<CreateProductItem>>(), Arg.Any<CancellationToken>())
            .Returns(TrendyolResponse<BatchRequestResponse>.Success(200, new BatchRequestResponse("batch-1")));

        var result = await _service.CreateAsync(new CreateTrendyolProductsRequest([TestProducts.Valid("A"), TestProducts.Valid("B")]), Ct);

        result.Value.ShouldBe(new BatchAcceptedDto("batch-1", MarketplaceBatchOperation.ProductCreate, 2));
        var tracked = await _context.MarketplaceBatchRequests.SingleAsync(Ct);
        tracked.BatchRequestId.ShouldBe("batch-1");
        tracked.Marketplace.ShouldBe("Trendyol");
        tracked.Status.ShouldBe(MarketplaceBatchStatus.Pending);
        tracked.ItemCount.ShouldBe(2);
    }

    [Fact]
    public async Task Failed_create_is_not_tracked()
    {
        _client.CreateProductsAsync(Arg.Any<IReadOnlyList<CreateProductItem>>(), Arg.Any<CancellationToken>())
            .Returns(TrendyolResponse<BatchRequestResponse>.Failure(400, new TrendyolError(null, [new TrendyolErrorDetail(null, "Kategori bulunamadı", null)], "{}")));

        var result = await _service.CreateAsync(new CreateTrendyolProductsRequest([TestProducts.Valid("A")]), Ct);

        result.Error!.Kind.ShouldBe(MarketplaceErrorKind.Validation);
        result.Error.Messages.ShouldBe(["Kategori bulunamadı"]);
        (await _context.MarketplaceBatchRequests.CountAsync(Ct)).ShouldBe(0);
    }

    [Fact]
    public async Task Price_inventory_update_is_tracked_with_operation()
    {
        _client.UpdatePriceAndInventoryAsync(Arg.Any<IReadOnlyList<PriceInventoryItem>>(), Arg.Any<CancellationToken>())
            .Returns(TrendyolResponse<BatchRequestResponse>.Success(200, new BatchRequestResponse("batch-2")));

        var result = await _service.UpdatePriceAndInventoryAsync(new UpdatePriceInventoryRequest([new PriceInventoryItem("A", Quantity: 3)]), Ct);

        result.Value!.Operation.ShouldBe(MarketplaceBatchOperation.PriceInventory);
        (await _context.MarketplaceBatchRequests.SingleAsync(Ct)).Operation.ShouldBe(MarketplaceBatchOperation.PriceInventory);
    }

    [Fact]
    public async Task Delete_content_and_unapproved_updates_are_tracked()
    {
        _client.DeleteProductsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(TrendyolResponse<BatchRequestResponse>.Success(200, new BatchRequestResponse("d")));
        _client.UpdateApprovedContentAsync(Arg.Any<IReadOnlyList<ApprovedContentUpdateItem>>(), Arg.Any<CancellationToken>()).Returns(TrendyolResponse<BatchRequestResponse>.Success(200, new BatchRequestResponse("c")));
        _client.UpdateUnapprovedProductsAsync(Arg.Any<IReadOnlyList<UnapprovedProductUpdateItem>>(), Arg.Any<CancellationToken>()).Returns(TrendyolResponse<BatchRequestResponse>.Success(200, new BatchRequestResponse("u")));

        await _service.DeleteAsync(new DeleteTrendyolProductsRequest(["A"]), Ct);
        await _service.UpdateContentAsync(new UpdateTrendyolContentRequest([new ApprovedContentUpdateItem { ContentId = 1, Title = "Yeni" }]), Ct);
        await _service.UpdateUnapprovedAsync(new UpdateUnapprovedTrendyolProductsRequest([new UnapprovedProductUpdateItem { Barcode = "A", Title = "Yeni" }]), Ct);

        (await _context.MarketplaceBatchRequests.Select(b => b.Operation).ToListAsync(Ct)).ShouldBe(
            [MarketplaceBatchOperation.ProductDelete, MarketplaceBatchOperation.ProductContentUpdate, MarketplaceBatchOperation.UnapprovedProductUpdate],
            ignoreOrder: true);
    }

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}

internal static class TestProducts
{
    public static CreateProductItem Valid(string barcode) => new()
    {
        Barcode = barcode,
        Title = "Tişört",
        ProductMainId = "MODEL-1",
        BrandId = 10,
        CategoryId = 2,
        Quantity = 5,
        Description = "<p>Pamuklu</p>",
        ListPrice = 200m,
        SalePrice = 150m,
        VatRate = 20,
        Images = [new TrendyolImage("https://cdn.example.com/a.jpg")],
        Attributes = [new TrendyolProductAttribute(293, AttributeValueId: 1001)]
    };
}
