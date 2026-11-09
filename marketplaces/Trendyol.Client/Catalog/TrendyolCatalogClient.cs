using Trendyol.Client.Http;

namespace Trendyol.Client.Catalog;

public interface ITrendyolCatalogClient
{
    Task<TrendyolResponse<TrendyolCategoryTree>> GetCategoryTreeAsync(CancellationToken cancellationToken = default);
    Task<TrendyolResponse<TrendyolCategoryAttributes>> GetCategoryAttributesAsync(long categoryId, CancellationToken cancellationToken = default);
    Task<TrendyolResponse<TrendyolAttributeValuePage>> GetAttributeValuesAsync(long categoryId, long attributeId, int page = 0, int size = 1000, CancellationToken cancellationToken = default);
    Task<TrendyolResponse<TrendyolBrandList>> GetBrandsAsync(int page = 0, int size = 1000, CancellationToken cancellationToken = default);
    Task<TrendyolResponse<IReadOnlyList<TrendyolBrand>>> SearchBrandsAsync(string name, CancellationToken cancellationToken = default);
}

/// <summary>
/// Kategori, kategori özelliği ve marka servisleri (servis limiti dakikada 50; sonuçlar önbelleğe alınmalı).
/// </summary>
public sealed class TrendyolCatalogClient(HttpClient httpClient) : ITrendyolCatalogClient
{
    private const string Prefix = "/integration/product";
    private readonly TrendyolHttp _http = new(httpClient);

    public Task<TrendyolResponse<TrendyolCategoryTree>> GetCategoryTreeAsync(CancellationToken cancellationToken = default) =>
        _http.GetAsync<TrendyolCategoryTree>($"{Prefix}/product-categories", cancellationToken);

    public Task<TrendyolResponse<TrendyolCategoryAttributes>> GetCategoryAttributesAsync(long categoryId, CancellationToken cancellationToken = default) =>
        _http.GetAsync<TrendyolCategoryAttributes>($"{Prefix}/categories/{categoryId}/attributes", cancellationToken);

    public Task<TrendyolResponse<TrendyolAttributeValuePage>> GetAttributeValuesAsync(long categoryId, long attributeId, int page = 0, int size = 1000, CancellationToken cancellationToken = default) =>
        _http.GetAsync<TrendyolAttributeValuePage>(
            new QueryString().Add("page", page).Add("size", size).Apply($"{Prefix}/categories/{categoryId}/attributes/{attributeId}/values"),
            cancellationToken);

    public Task<TrendyolResponse<TrendyolBrandList>> GetBrandsAsync(int page = 0, int size = 1000, CancellationToken cancellationToken = default) =>
        _http.GetAsync<TrendyolBrandList>(new QueryString().Add("page", page).Add("size", size).Apply($"{Prefix}/brands"), cancellationToken);

    public Task<TrendyolResponse<IReadOnlyList<TrendyolBrand>>> SearchBrandsAsync(string name, CancellationToken cancellationToken = default) =>
        _http.GetAsync<IReadOnlyList<TrendyolBrand>>(new QueryString().Add("name", name).Apply($"{Prefix}/brands/by-name"), cancellationToken);
}
