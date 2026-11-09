using ECommerce.Core.Persistence;
using ECommerce.Marketplaces;
using Trendyol.Client;
using Trendyol.Client.Products;

namespace ECommerce.Application.Marketplaces.Trendyol;

/// <summary>
/// Trendyol'un döndürdüğü batchRequestId'yi durum takibi için kaydeder.
/// </summary>
public class TrendyolBatchTracker(IRepository<MarketplaceBatchRequest> batches, IUnitOfWork unitOfWork)
{
    public const string Marketplace = "Trendyol";

    public async Task<MarketplaceResult<BatchAcceptedDto>> TrackAsync(
        TrendyolResponse<BatchRequestResponse> response, MarketplaceBatchOperation operation, int itemCount, CancellationToken cancellationToken)
    {
        if (!response.IsSuccess)
            return MarketplaceResult<BatchAcceptedDto>.Failure(MarketplaceResultExtensions.ToError(response.StatusCode, response.Error));

        var batchRequestId = response.Data!.BatchRequestId;
        batches.Add(MarketplaceBatchRequest.Create(Marketplace, batchRequestId, operation, itemCount));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MarketplaceResult<BatchAcceptedDto>.Success(new BatchAcceptedDto(batchRequestId, operation, itemCount));
    }
}
