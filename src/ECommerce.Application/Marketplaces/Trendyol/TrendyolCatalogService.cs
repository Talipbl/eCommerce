using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using Trendyol.Client.Catalog;

namespace ECommerce.Application.Marketplaces.Trendyol;

/// <summary>
/// Trendyol referans verileri. Servis limiti düşük olduğu için başarılı cevaplar önbelleğe alınır, hatalar alınmaz.
/// </summary>
public class TrendyolCatalogService(ITrendyolCatalogClient client, IMemoryCache cache, IMapper mapper) : ITrendyolCatalogService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);
    private static readonly CompareInfo Turkish = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;

    public Task<MarketplaceResult<IReadOnlyList<CategoryNodeDto>>> GetCategoryTreeAsync(CancellationToken cancellationToken = default) =>
        CachedAsync("trendyol:categories", async () =>
            (await client.GetCategoryTreeAsync(cancellationToken))
                .ToMarketplaceResult<TrendyolCategoryTree, IReadOnlyList<CategoryNodeDto>>(tree => (tree.Categories ?? []).Select(ToNode).ToList()));

    public async Task<MarketplaceResult<IReadOnlyList<CategorySearchResultDto>>> SearchCategoriesAsync(string term, int limit = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
            return MarketplaceResult<IReadOnlyList<CategorySearchResultDto>>.Failure(MarketplaceError.Validation("Arama terimi en az 2 karakter olmalıdır."));

        var tree = await GetCategoryTreeAsync(cancellationToken);
        if (!tree.Succeeded)
            return MarketplaceResult<IReadOnlyList<CategorySearchResultDto>>.Failure(tree.Error!);

        var matches = Leaves(tree.Value!, [])
            .Where(x => Turkish.IndexOf(x.Node.Name, term.Trim(), CompareOptions.IgnoreCase) >= 0)
            .Take(limit)
            .Select(x => new CategorySearchResultDto(x.Node.Id, x.Node.Name, x.Path))
            .ToList();

        return MarketplaceResult<IReadOnlyList<CategorySearchResultDto>>.Success(matches);
    }

    public Task<MarketplaceResult<IReadOnlyList<CategoryAttributeDto>>> GetCategoryAttributesAsync(long categoryId, CancellationToken cancellationToken = default) =>
        CachedAsync($"trendyol:categories:{categoryId}:attributes", async () =>
            (await client.GetCategoryAttributesAsync(categoryId, cancellationToken))
                .ToMarketplaceResult<TrendyolCategoryAttributes, IReadOnlyList<CategoryAttributeDto>>(a => (a.CategoryAttributes ?? [])
                    .Select(mapper.Map<CategoryAttributeDto>)
                    .ToList()));

    public Task<MarketplaceResult<IReadOnlyList<AttributeValueDto>>> GetAttributeValuesAsync(long categoryId, long attributeId, CancellationToken cancellationToken = default) =>
        CachedAsync($"trendyol:categories:{categoryId}:attributes:{attributeId}:values", async () =>
        {
            const int pageSize = 1000;
            var values = new List<AttributeValueDto>();

            for (var page = 0; ; page++)
            {
                var response = await client.GetAttributeValuesAsync(categoryId, attributeId, page, pageSize, cancellationToken);
                if (!response.IsSuccess)
                    return MarketplaceResult<IReadOnlyList<AttributeValueDto>>.Failure(MarketplaceResultExtensions.ToError(response.StatusCode, response.Error));

                values.AddRange((response.Data!.Content ?? []).Select(mapper.Map<AttributeValueDto>));
                if (page + 1 >= response.Data.TotalPages)
                    break;
            }

            return MarketplaceResult<IReadOnlyList<AttributeValueDto>>.Success(values);
        });

    public async Task<MarketplaceResult<IReadOnlyList<TrendyolBrandDto>>> SearchBrandsAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return MarketplaceResult<IReadOnlyList<TrendyolBrandDto>>.Failure(MarketplaceError.Validation("Marka adı boş olamaz."));

        return await CachedAsync($"trendyol:brands:{name.Trim()}", async () =>
            (await client.SearchBrandsAsync(name.Trim(), cancellationToken))
                .ToMarketplaceResult<IReadOnlyList<TrendyolBrand>, IReadOnlyList<TrendyolBrandDto>>(brands => brands.Select(mapper.Map<TrendyolBrandDto>).ToList()));
    }

    private async Task<MarketplaceResult<T>> CachedAsync<T>(string key, Func<Task<MarketplaceResult<T>>> factory)
    {
        if (cache.TryGetValue(key, out MarketplaceResult<T>? cached) && cached is not null)
            return cached;

        var result = await factory();
        if (result.Succeeded)
            cache.Set(key, result, CacheDuration);

        return result;
    }

    private static CategoryNodeDto ToNode(TrendyolCategory category) =>
        new(category.Id, category.Name, category.ParentId, category.IsLeaf, (category.SubCategories ?? []).Select(ToNode).ToList());

    private static IEnumerable<(CategoryNodeDto Node, string Path)> Leaves(IEnumerable<CategoryNodeDto> nodes, IReadOnlyList<string> parents)
    {
        foreach (var node in nodes)
        {
            List<string> path = [.. parents, node.Name];
            if (node.IsLeaf)
            {
                if (parents.Count > 0)
                    yield return (node, string.Join(" > ", path));
                continue;
            }

            foreach (var leaf in Leaves(node.SubCategories, path))
                yield return leaf;
        }
    }
}
