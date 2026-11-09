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
