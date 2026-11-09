using ECommerce.AdminPanel.Api;

namespace ECommerce.AdminPanel.Models;

public sealed record OutboxPage(OutboxKind Kind, OutboxStatus? Status, PagedResultOfOutboxEntryDto Result)
{
    public PagerModel Pager => new(Result.Page, Result.TotalPages ?? 0, Result.TotalElements);
}
