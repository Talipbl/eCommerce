using ECommerce.AdminPanel.Api;

namespace ECommerce.AdminPanel.Infrastructure;

/// <summary>Ağaçtaki yeri ile düzleştirilmiş kategori; seçim listelerinde girintiyle gösterilir.</summary>
public sealed record CategoryOption(int Id, string Name, int Depth, bool IsActive, IReadOnlyList<int> AncestorIds)
{
    public string IndentedName => $"{new string('·', Depth * 2)}{(Depth > 0 ? " " : "")}{Name}";
}

/// <summary>Formlarda ve listelerde kullanılan marka ve kategori listeleri; istek boyunca bir kez çekilir.</summary>
public sealed class CatalogLookups(IBrandsClient brands, ICategoriesClient categories)
{
    private IReadOnlyList<BrandDto>? _brands;
    private IReadOnlyList<CategoryOption>? _categories;

    public async Task<IReadOnlyList<BrandDto>> BrandsAsync(CancellationToken cancellationToken) =>
        _brands ??= (await brands.ListAsync(null, cancellationToken)).OrderBy(b => b.Name, Display.TurkishOrder).ToList();

    public async Task<IReadOnlyList<CategoryOption>> CategoriesAsync(CancellationToken cancellationToken) =>
        _categories ??= Flatten(await categories.GetTreeAsync(cancellationToken));

    public static IReadOnlyList<CategoryOption> Flatten(IEnumerable<CategoryTreeDto> tree)
    {
        var result = new List<CategoryOption>();
        void Visit(IEnumerable<CategoryTreeDto> nodes, int depth, IReadOnlyList<int> ancestors)
        {
            foreach (var node in nodes.OrderBy(n => n.SortOrder).ThenBy(n => n.Name, Display.TurkishOrder))
            {
                result.Add(new CategoryOption(node.Id, node.Name, depth, node.IsActive, ancestors));
                Visit(node.Children ?? [], depth + 1, [.. ancestors, node.Id]);
            }
        }

        Visit(tree, 0, []);
        return result;
    }
}
