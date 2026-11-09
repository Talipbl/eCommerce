using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;
using ECommerce.AdminPanel.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.AdminPanel.Controllers;

/// <summary>Yerel marka/kategorilerin Trendyol karşılıkları ve kategori özellik eşlemesi.</summary>
[Route("trendyol")]
public class TrendyolMappingsController(ITrendyolIntegrationClient integration, ITrendyolCatalogClient catalog, CatalogLookups lookups) : PanelController
{
    private const int SearchLimit = 30;

    [HttpGet("brands")]
    public async Task<IActionResult> Brands(CancellationToken cancellationToken)
    {
        var mappings = await integration.ListBrandMappingsAsync(cancellationToken);
        var rows = (await lookups.BrandsAsync(cancellationToken)).Select(b =>
        {
            var mapping = mappings.FirstOrDefault(m => m.BrandId == b.Id);
            return new MappingRow(b.Id, b.Name, 0, mapping?.ExternalBrandId, mapping?.ExternalBrandName);
        }).ToList();
        return View(rows);
    }

    [HttpGet("brands/{brandId:int}")]
    public async Task<IActionResult> MapBrand(int brandId, string? q, CancellationToken cancellationToken)
    {
        var brand = (await lookups.BrandsAsync(cancellationToken)).FirstOrDefault(b => b.Id == brandId);
        if (brand is null)
            return NotFound();

        var query = string.IsNullOrWhiteSpace(q) ? brand.Name : q.Trim();
        var mapping = (await integration.ListBrandMappingsAsync(cancellationToken)).FirstOrDefault(m => m.BrandId == brandId);
        var local = new MappingRow(brand.Id, brand.Name, 0, mapping?.ExternalBrandId, mapping?.ExternalBrandName);
        try
        {
            var results = await catalog.SearchBrandsAsync(query, cancellationToken);
            return View("MapSearch", new MappingSearchPage(local, query, results.Take(SearchLimit).Select(r => new MappingSearchResult(r.Id, r.Name, null)).ToList(), null));
        }
        catch (ApiException exception)
        {
            return View("MapSearch", new MappingSearchPage(local, query, [], ApiProblem.From(exception).Describe()));
        }
    }

    [HttpPost("brands/{brandId:int}")]
    public async Task<IActionResult> MapBrand(int brandId, long externalId, string? externalName, CancellationToken cancellationToken)
    {
        try
        {
            await integration.MapBrandAsync(brandId, new MapBrandRequest { ExternalBrandId = externalId, ExternalBrandName = externalName }, cancellationToken);
            FlashSuccess($"Marka Trendyol'daki {externalName} ile eşlendi.");
            return RedirectToAction(nameof(Brands));
        }
        catch (ApiException exception)
        {
            FlashError(exception);
            return RedirectToAction(nameof(MapBrand), new { brandId });
        }
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken)
    {
        var mappings = await integration.ListCategoryMappingsAsync(cancellationToken);
        var rows = (await lookups.CategoriesAsync(cancellationToken)).Select(c =>
        {
            var mapping = mappings.FirstOrDefault(m => m.CategoryId == c.Id);
            return new MappingRow(c.Id, c.Name, c.Depth, mapping?.ExternalCategoryId, mapping?.ExternalCategoryName, mapping?.Attributes.Count ?? 0);
        }).ToList();
        return View(rows);
    }

    [HttpGet("categories/{categoryId:int}")]
    public async Task<IActionResult> MapCategory(int categoryId, string? q, CancellationToken cancellationToken)
    {
        var local = await CategoryRowAsync(categoryId, cancellationToken);
        if (local is null)
            return NotFound();

        var query = string.IsNullOrWhiteSpace(q) ? local.LocalName : q.Trim();
        try
        {
            var results = await catalog.SearchCategoriesAsync(query, SearchLimit, cancellationToken);
            return View("MapSearch", new MappingSearchPage(local, query, results.Select(r => new MappingSearchResult(r.Id, r.Name, r.Path)).ToList(), null));
        }
        catch (ApiException exception)
        {
            return View("MapSearch", new MappingSearchPage(local, query, [], ApiProblem.From(exception).Describe()));
        }
    }

    [HttpPost("categories/{categoryId:int}")]
    public async Task<IActionResult> MapCategory(int categoryId, long externalId, string? externalName, CancellationToken cancellationToken)
    {
        try
        {
            await integration.MapCategoryAsync(categoryId, new MapCategoryRequest { ExternalCategoryId = externalId, ExternalCategoryName = externalName }, cancellationToken);
            FlashSuccess($"Kategori Trendyol'daki {externalName} ile eşlendi. Şimdi özellikleri eşleyin.");
            return RedirectToAction(nameof(Attributes), new { categoryId });
        }
        catch (ApiException exception)
        {
            FlashError(exception);
            return RedirectToAction(nameof(MapCategory), new { categoryId });
        }
    }

    [HttpGet("categories/{categoryId:int}/attributes")]
    public async Task<IActionResult> Attributes(int categoryId, bool all, CancellationToken cancellationToken) =>
        await AttributesViewAsync(categoryId, all, cancellationToken);

    [HttpPost("categories/{categoryId:int}/attributes")]
    public async Task<IActionResult> Attributes(int categoryId, bool all, List<AttributeMappingRow> rows, CancellationToken cancellationToken)
    {
        AttributeMappingPage? context;
        try
        {
            context = await LoadAttributesAsync(categoryId, all, cancellationToken);
        }
        catch (ApiException exception)
        {
            FlashError(exception);
            return RedirectToAction(nameof(Categories));
        }

        if (context is null)
            return RedirectToAction(nameof(MapCategory), new { categoryId });

        var request = AttributeMappingPage.ToRequest(context.Attributes, rows, ModelState);
        if (ModelState.IsValid)
        {
            try
            {
                await integration.MapCategoryAttributesAsync(categoryId, request, cancellationToken);
                FlashSuccess($"{request.Attributes.Count} özellik eşlemesi kaydedildi.");
                return RedirectToAction(nameof(Attributes), new { categoryId, all });
            }
            catch (ApiException exception)
            {
                ModelState.AddApiProblem(exception);
            }
        }

        var postedRows = context.Attributes
            .Select(a => rows.FirstOrDefault(r => r.ExternalAttributeId == a.Attribute.Id) ?? new AttributeMappingRow { ExternalAttributeId = a.Attribute.Id })
            .ToList();
        return View("Attributes", context with { Rows = postedRows });
    }

    private async Task<IActionResult> AttributesViewAsync(int categoryId, bool all, CancellationToken cancellationToken)
    {
        try
        {
            var context = await LoadAttributesAsync(categoryId, all, cancellationToken);
            if (context is not null)
                return View("Attributes", context);

            FlashError("Kategori önce bir Trendyol kategorisiyle eşleştirilmelidir.");
            return RedirectToAction(nameof(MapCategory), new { categoryId });
        }
        catch (ApiException exception)
        {
            FlashError(exception);
            return RedirectToAction(nameof(Categories));
        }
    }

    /// <summary>
    /// Zorunlu, varyant belirleyen ve zaten eşlenmiş özellikler her zaman, diğerleri <paramref name="all"/> ile gösterilir.
    /// Değer listeleri yalnızca gösterilen özellikler için (API önbelleğinden, paralel) çekilir.
    /// </summary>
    private async Task<AttributeMappingPage?> LoadAttributesAsync(int categoryId, bool all, CancellationToken cancellationToken)
    {
        var mapping = (await integration.ListCategoryMappingsAsync(cancellationToken)).FirstOrDefault(m => m.CategoryId == categoryId);
        var category = (await lookups.CategoriesAsync(cancellationToken)).FirstOrDefault(c => c.Id == categoryId);
        if (mapping is null || category is null)
            return null;

        var mappedIds = mapping.Attributes.Select(a => a.ExternalAttributeId).ToHashSet();
        var definitions = (await catalog.GetCategoryAttributesAsync(mapping.ExternalCategoryId, cancellationToken))
            .OrderByDescending(d => d.Required).ThenByDescending(d => d.Varianter).ThenBy(d => d.Name, Display.TurkishOrder)
            .ToList();
        var shown = definitions.Where(d => all || d.Required || d.Varianter || mappedIds.Contains(d.Id)).ToList();
        var attributes = await Task.WhenAll(shown.Select(async d =>
            new TrendyolAttribute(d, (await catalog.GetAttributeValuesAsync(mapping.ExternalCategoryId, d.Id, cancellationToken)).ToList())));

        var local = new MappingRow(category.Id, category.Name, category.Depth, mapping.ExternalCategoryId, mapping.ExternalCategoryName, mapping.Attributes.Count);
        return new AttributeMappingPage(local, attributes, AttributeMappingPage.RowsFrom(attributes, mapping), all, definitions.Count - shown.Count);
    }

    private async Task<MappingRow?> CategoryRowAsync(int categoryId, CancellationToken cancellationToken)
    {
        var category = (await lookups.CategoriesAsync(cancellationToken)).FirstOrDefault(c => c.Id == categoryId);
        if (category is null)
            return null;

        var mapping = (await integration.ListCategoryMappingsAsync(cancellationToken)).FirstOrDefault(m => m.CategoryId == categoryId);
        return new MappingRow(category.Id, category.Name, category.Depth, mapping?.ExternalCategoryId, mapping?.ExternalCategoryName, mapping?.Attributes.Count ?? 0);
    }
}
