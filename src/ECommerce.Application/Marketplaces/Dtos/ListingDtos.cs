using ECommerce.Marketplaces;

namespace ECommerce.Application.Marketplaces;

public record PublishListingRequest(
    IReadOnlyList<ListingAttribute>? ProductAttributes = null,
    IReadOnlyList<VariantListingAttributes>? VariantAttributes = null,
    int? DeliveryDuration = null);

public record ListingDto(
    int ProductId,
    ListingStatus Status,
    long? ExternalContentId,
    string? LastBatchRequestId,
    string? StatusMessage,
    DateTime? LastSubmittedAt,
    DateTime? LastCheckedAt,
    IReadOnlyList<ListingAttribute> ProductAttributes,
    IReadOnlyList<VariantListingAttributes> VariantAttributes,
    int? DeliveryDuration);

public record ListingQuery(ListingStatus? Status, int Page = 0, int Size = 20);

public record ListingSummaryDto(
    int ProductId,
    string ProductName,
    string ProductCode,
    ListingStatus Status,
    long? ExternalContentId,
    string? LastBatchRequestId,
    string? StatusMessage,
    DateTime? LastSubmittedAt,
    DateTime? LastCheckedAt);
