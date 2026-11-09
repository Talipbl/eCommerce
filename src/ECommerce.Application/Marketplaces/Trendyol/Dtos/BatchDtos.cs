using ECommerce.Marketplaces;

namespace ECommerce.Application.Marketplaces.Trendyol;

public record BatchDto(
    string BatchRequestId,
    MarketplaceBatchOperation Operation,
    MarketplaceBatchStatus Status,
    int ItemCount,
    int FailedItemCount,
    DateTime CreatedAt,
    DateTime? LastCheckedAt,
    DateTime? CompletedAt,
    IReadOnlyList<MarketplaceBatchFailure> Failures);
