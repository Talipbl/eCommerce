using ECommerce.AdminPanel.Api;

namespace ECommerce.AdminPanel.Models;

public sealed record SyncLogPage(SyncOperation? Operation, bool? Succeeded, PagedResultOfSyncLogDto Result)
{
    public PagerModel Pager => new(Result.Page, Result.TotalPages ?? 0, Result.TotalElements);
}
