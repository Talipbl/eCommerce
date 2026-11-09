using ECommerce.AdminPanel.Api;

namespace ECommerce.AdminPanel.Models;

/// <summary>Sipariş listesi filtreleri; site ve pazaryeri siparişleri aynı listededir.</summary>
public sealed record OrderFilter(OrderStatus? Status, OrderChannel? Channel, string? Search, int Page);
