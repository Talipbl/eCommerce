using ECommerce.AdminPanel.Api;

namespace ECommerce.AdminPanel.Models;

public sealed record ListingListPage(ListingStatus? Status, PagedResultOfListingSummaryDto Result)
{
    public PagerModel Pager => new(Result.Page, Result.TotalPages ?? 0, Result.TotalElements);
}
