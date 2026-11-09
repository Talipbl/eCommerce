using ECommerce.AdminPanel.Api;

namespace ECommerce.AdminPanel.Models;

public sealed record OrderListPage(OrderFilter Filter, PagedResultOfOrderSummaryDto Result)
{
    public PagerModel Pager => new(Result.Page, Result.TotalPages ?? 0, Result.TotalElements);
}
