using ECommerce.AdminPanel.Api;

namespace ECommerce.AdminPanel.Models;

/// <summary>Panel ana sayfası; özet alınamazsa sayfa yine açılır ve hata gösterilir (diğer ekranlara geçilebilsin).</summary>
public sealed record DashboardPage(DashboardSummaryDto? Summary, string? Error)
{
    public int Count(OrderStatus status) => Summary?.OrdersByStatus.FirstOrDefault(s => s.Status == status)?.Count ?? 0;

    public int FailedOutboxTotal => (Summary?.FailedOutbox.Inventory ?? 0) + (Summary?.FailedOutbox.OrderStatus ?? 0);
}
