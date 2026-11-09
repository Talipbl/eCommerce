using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;
using ECommerce.AdminPanel.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.AdminPanel.Controllers;

[Route("products")]
public class ProductsController(IProductsClient products, ITrendyolIntegrationClient trendyol, CatalogLookups lookups) : PanelController
{
    private const int PageSize = 20;
    private const long MaxUploadSize = 5 * 1024 * 1024;
    private const string DetailsPrefix = "Details";
    private const string NewVariantPrefix = "NewVariant";

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int? categoryId, int? brandId, bool? isActive, int page, CancellationToken cancellationToken)
    {
        var filter = new ProductFilter(string.IsNullOrWhiteSpace(search) ? null : search.Trim(), categoryId, brandId, isActive, Math.Max(page, 0));
        var result = await products.ListAsync(filter.Search, filter.CategoryId, filter.BrandId, filter.IsActive, filter.Page, PageSize, cancellationToken);
        return View(new ProductListPage(filter, result, await lookups.BrandsAsync(cancellationToken), await lookups.CategoriesAsync(cancellationToken)));
    }

    [HttpGet("new")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        await CreateViewAsync(new ProductDetailsForm(), new VariantForm(), cancellationToken);

    [HttpPost("new")]
    public async Task<IActionResult> Create(
        [Bind(Prefix = DetailsPrefix)] ProductDetailsForm details, [Bind(Prefix = "Variant")] VariantForm variant, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return await CreateViewAsync(details, variant, cancellationToken);

        try
        {
            var created = await products.CreateAsync(details.ToCreateRequest(variant.ToInput()), cancellationToken);
            FlashSuccess("Ürün oluşturuldu. Görselleri ekleyip yayınlayabilirsiniz.");
            return RedirectToAction(nameof(Edit), new { id = created.Id });
        }
        catch (ApiException exception)
        {
            ModelState.AddApiProblem(exception, mapKey: MapCreateKey);
            return await CreateViewAsync(details, variant, cancellationToken);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken) =>
        await EditViewAsync(id, null, null, cancellationToken);

    [HttpPost("{id:int}")]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = DetailsPrefix)] ProductDetailsForm details, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return await EditViewAsync(id, details, null, cancellationToken);

        try
        {
            await products.UpdateAsync(id, details.ToUpdateRequest(), cancellationToken);
            FlashSuccess("Ürün bilgileri kaydedildi.");
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (ApiException exception)
        {
            ModelState.AddApiProblem(exception, DetailsPrefix);
            return await EditViewAsync(id, details, null, cancellationToken);
        }
    }

    [HttpPost("{id:int}/publish")]
    public Task<IActionResult> Publish(int id, CancellationToken cancellationToken) =>
        RunAsync(id, () => products.PublishAsync(id, cancellationToken), "Ürün yayına alındı.");

    [HttpPost("{id:int}/unpublish")]
    public Task<IActionResult> Unpublish(int id, CancellationToken cancellationToken) =>
        RunAsync(id, () => products.UnpublishAsync(id, cancellationToken), "Ürün yayından kaldırıldı.");

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await products.DeleteAsync(id, cancellationToken);
            FlashSuccess("Ürün silindi.");
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException exception)
        {
            FlashError(exception);
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    [HttpPost("{id:int}/variants")]
    public async Task<IActionResult> AddVariant(int id, [Bind(Prefix = NewVariantPrefix)] VariantForm variant, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return await EditViewAsync(id, null, variant, cancellationToken);

        try
        {
            await products.AddVariantAsync(id, variant.ToInput(), cancellationToken);
            FlashSuccess($"{variant.Sku} varyantı eklendi.");
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (ApiException exception)
        {
            ModelState.AddApiProblem(exception, NewVariantPrefix, VariantAttributesText.MapApiKey);
            return await EditViewAsync(id, null, variant, cancellationToken);
        }
    }

    [HttpPost("{id:int}/variants/{variantId:int}")]
    public async Task<IActionResult> UpdateVariant(int id, int variantId, VariantUpdateForm variant, CancellationToken cancellationToken)
    {
        // Satır formlarında hata alan bazında değil, sayfanın üstünde gösterilir.
        if (!ModelState.IsValid)
        {
            FlashError(string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return RedirectToAction(nameof(Edit), new { id });
        }

        return await RunAsync(id, () => products.UpdateVariantAsync(id, variantId, variant.ToRequest(), cancellationToken), "Varyant güncellendi.");
    }

    [HttpPost("{id:int}/variants/{variantId:int}/delete")]
    public Task<IActionResult> RemoveVariant(int id, int variantId, CancellationToken cancellationToken) =>
        RunAsync(id, () => products.RemoveVariantAsync(id, variantId, cancellationToken), "Varyant silindi.");

    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(MaxUploadSize + 64 * 1024)]
    public async Task<IActionResult> UploadImage(int id, IFormFile? file, string? altText, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            FlashError("Yüklenecek görseli seçin.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        await using var stream = file.OpenReadStream();
        var parameter = new FileParameter(stream, file.FileName, file.ContentType);
        return await RunAsync(id, () => products.UploadImageAsync(id, parameter, string.IsNullOrWhiteSpace(altText) ? null : altText.Trim(), cancellationToken),
            "Görsel yüklendi.");
    }

    [HttpPost("{id:int}/images/{imageId:int}/delete")]
    public Task<IActionResult> DeleteImage(int id, int imageId, CancellationToken cancellationToken) =>
        RunAsync(id, () => products.DeleteImageAsync(id, imageId, cancellationToken), "Görsel silindi.");

    /// <summary>
    /// Sıralama formu ekrandaki sırayı <paramref name="imageIds"/> ile gönderir (sürükle-bırak).
    /// JavaScript olmadan da çalışsın diye ok düğmeleri <paramref name="move"/> ile "görselId:yön" gönderir.
    /// </summary>
    [HttpPost("{id:int}/images/order")]
    public Task<IActionResult> ReorderImages(int id, List<int> imageIds, string? move, CancellationToken cancellationToken)
    {
        var order = ImageOrder.Apply(imageIds, move);
        return RunAsync(id, () => products.ReorderImagesAsync(id, new ReorderImagesRequest { ImageIds = order }, cancellationToken), "Görsel sırası kaydedildi.");
    }

    /// <summary>
    /// Ürünü kategori ve marka eşlemeleriyle Trendyol'a gönderir; özellikler kategori eşlemesinden çözülür.
    /// Eksik eşleme veya zorunlu özellik API tarafından reddedilir ve sebep bildirim olarak gösterilir.
    /// </summary>
    [HttpPost("{id:int}/trendyol")]
    public Task<IActionResult> PublishToTrendyol(int id, int? deliveryDuration, CancellationToken cancellationToken) =>
        RunAsync(id, () => trendyol.PublishAsync(id, new PublishListingRequest { DeliveryDuration = deliveryDuration }, cancellationToken),
            "Ürün Trendyol'a gönderildi. Sonuç birkaç dakika içinde gelir; Trendyol yayınları sayfasından takip edebilirsiniz.");

    // Formsuz işlemler: sonuç ürün sayfasında bildirim olarak gösterilir.
    private async Task<IActionResult> RunAsync(int id, Func<Task> action, string success)
    {
        try
        {
            await action();
            FlashSuccess(success);
        }
        catch (ApiException exception)
        {
            FlashError(exception);
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    private async Task<IActionResult> CreateViewAsync(ProductDetailsForm details, VariantForm variant, CancellationToken cancellationToken) =>
        View("Create", new ProductCreatePage(details, variant, await lookups.BrandsAsync(cancellationToken), await lookups.CategoriesAsync(cancellationToken)));

    private async Task<IActionResult> EditViewAsync(int id, ProductDetailsForm? details, VariantForm? newVariant, CancellationToken cancellationToken)
    {
        ProductDetailDto product;
        try
        {
            product = await products.GetAsync(id, cancellationToken);
        }
        catch (ApiException exception) when (exception.StatusCode == StatusCodes.Status404NotFound)
        {
            return NotFound();
        }

        return View("Edit", new ProductEditPage(
            product,
            details ?? ProductDetailsForm.From(product),
            newVariant ?? new VariantForm(),
            await lookups.BrandsAsync(cancellationToken),
            await lookups.CategoriesAsync(cancellationToken),
            await FindListingAsync(id, cancellationToken)));
    }

    private async Task<ListingDto?> FindListingAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await trendyol.GetListingAsync(id, cancellationToken);
        }
        catch (ApiException exception) when (exception.StatusCode == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    // API oluşturma isteğinde varyantlar liste: "Variants[0].Sku" → "Variant.Sku", ürün alanları → "Details.Name".
    private static string MapCreateKey(string key)
    {
        const string variantPrefix = "Variants[0].";
        if (key.StartsWith(variantPrefix, StringComparison.OrdinalIgnoreCase))
            return $"Variant.{VariantAttributesText.MapApiKey(key[variantPrefix.Length..])}";
        return key.StartsWith("Variants", StringComparison.OrdinalIgnoreCase) ? "Variant.Sku" : $"{DetailsPrefix}.{key}";
    }
}
