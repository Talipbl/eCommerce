using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;

namespace ECommerce.AdminPanel.Models;

public sealed record ProductListPage(
    ProductFilter Filter,
    PagedResultOfProductListItemDto Result,
    IReadOnlyList<BrandDto> Brands,
    IReadOnlyList<CategoryOption> Categories)
{
    public string BrandName(int id) => Brands.FirstOrDefault(b => b.Id == id)?.Name ?? "—";

    public string CategoryName(int id) => Categories.FirstOrDefault(c => c.Id == id)?.Name ?? "—";

    public PagerModel Pager => new(Result.Page, Result.TotalPages ?? 0, Result.TotalElements);
}
