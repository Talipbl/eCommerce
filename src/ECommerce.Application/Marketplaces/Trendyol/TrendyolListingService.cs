using ECommerce.Application.Catalog;
using ECommerce.Application.Common;
using ECommerce.Catalog;
using ECommerce.Core.Persistence;
using ECommerce.Marketplaces;
using FluentValidation;
using Trendyol.Client.Products;

namespace ECommerce.Application.Marketplaces.Trendyol;

public record MapCategoryRequest(long ExternalCategoryId, string? ExternalCategoryName);

public record MapBrandRequest(long ExternalBrandId, string? ExternalBrandName);

public record CategoryMappingDto(int CategoryId, long ExternalCategoryId, string? ExternalCategoryName, IReadOnlyList<CategoryAttributeMapping> Attributes);

public record SetCategoryAttributesRequest(IReadOnlyList<CategoryAttributeMapping> Attributes);

public record BrandMappingDto(int BrandId, long ExternalBrandId, string? ExternalBrandName);

public interface ITrendyolListingService
{
    Task<IReadOnlyList<CategoryMappingDto>> ListCategoryMappingsAsync(CancellationToken cancellationToken = default);
    Task<Result<CategoryMappingDto>> MapCategoryAsync(int categoryId, MapCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result<CategoryMappingDto>> SetCategoryAttributesAsync(int categoryId, SetCategoryAttributesRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BrandMappingDto>> ListBrandMappingsAsync(CancellationToken cancellationToken = default);
    Task<Result<BrandMappingDto>> MapBrandAsync(int brandId, MapBrandRequest request, CancellationToken cancellationToken = default);
    Task<Result<ListingDto>> GetAsync(int productId, CancellationToken cancellationToken = default);
    Task<Result<ListingDto>> PublishAsync(int productId, PublishListingRequest request, CancellationToken cancellationToken = default);
    Task<int> RefreshStatusesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Yerel ürünleri Trendyol'a yayınlar ve listing durumunu batch sonucu ile onay durumuna göre günceller.
/// </summary>
public class TrendyolListingService(
    IRepository<MarketplaceListing> listings,
    IRepository<MarketplaceCategoryMapping> categoryMappings,
    IRepository<MarketplaceBrandMapping> brandMappings,
    IRepository<MarketplaceBatchRequest> batches,
    IRepository<Product> products,
    IRepository<Category> categories,
    IRepository<Brand> brands,
    IRepository<MarketplaceSyncLog> logs,
    IUnitOfWork unitOfWork,
    ITrendyolProductService trendyolProducts,
    ITrendyolCatalogService trendyolCatalog,
    IMediaUrlBuilder media,
    TimeProvider clock) : ITrendyolListingService
{
    private const string Marketplace = MarketplaceNames.Trendyol;
    private const int MaxListingsPerRefresh = 20;

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<IReadOnlyList<CategoryMappingDto>> ListCategoryMappingsAsync(CancellationToken cancellationToken = default) =>
        (await categoryMappings.ListAsync(m => m.Marketplace == Marketplace, cancellationToken)).Select(ToDto).ToList();

    public async Task<Result<CategoryMappingDto>> MapCategoryAsync(int categoryId, MapCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (!await categories.AnyAsync(c => c.Id == categoryId, cancellationToken))
            return Result<CategoryMappingDto>.NotFound("Kategori bulunamadı.");

        var tree = await trendyolCatalog.GetCategoryTreeAsync(cancellationToken);
        if (!tree.Succeeded)
            return ToAppError(tree.Error!);
        var external = Flatten(tree.Value!).FirstOrDefault(c => c.Id == request.ExternalCategoryId);
        if (external is null || !external.IsLeaf)
            return Result<CategoryMappingDto>.Invalid("Trendyol'da ürün yalnızca en alt seviye (leaf) kategoriye eklenebilir.");

        var mapping = await categoryMappings.FirstOrDefaultAsync(m => m.Marketplace == Marketplace && m.CategoryId == categoryId, cancellationToken);
        var name = request.ExternalCategoryName ?? external.Name;
        if (mapping is null)
            categoryMappings.Add(mapping = MarketplaceCategoryMapping.Create(Marketplace, categoryId, request.ExternalCategoryId, name));
        else
            mapping.Update(request.ExternalCategoryId, name);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CategoryMappingDto>.Success(ToDto(mapping));
    }

    public async Task<Result<CategoryMappingDto>> SetCategoryAttributesAsync(int categoryId, SetCategoryAttributesRequest request, CancellationToken cancellationToken = default)
    {
        var mapping = await categoryMappings.FirstOrDefaultAsync(m => m.Marketplace == Marketplace && m.CategoryId == categoryId, cancellationToken);
        if (mapping is null)
            return Result<CategoryMappingDto>.NotFound("Kategori önce bir Trendyol kategorisiyle eşleştirilmelidir.");

        var attributes = await trendyolCatalog.GetCategoryAttributesAsync(mapping.ExternalCategoryId, cancellationToken);
        if (!attributes.Succeeded)
            return ToAppError(attributes.Error!);

        var byId = attributes.Value!.ToDictionary(a => a.Id);
        foreach (var item in request.Attributes)
        {
            if (!byId.TryGetValue(item.ExternalAttributeId, out var attribute))
                return Result<CategoryMappingDto>.Invalid($"{item.ExternalAttributeId} numaralı özellik Trendyol kategorisinde yok.");

            var custom = item.Values.Select(v => v.CustomValue).Append(item.DefaultCustomValue).Any(v => !string.IsNullOrWhiteSpace(v));
            if (custom && !attribute.AllowCustom)
                return Result<CategoryMappingDto>.Invalid($"{attribute.Name} özelliği serbest değer kabul etmiyor; değer numarası seçin.");

            var valueIds = item.Values.Select(v => v.ValueId).Append(item.DefaultValueId).OfType<long>().Distinct().ToList();
            if (valueIds.Count == 0)
                continue;
            var values = await trendyolCatalog.GetAttributeValuesAsync(mapping.ExternalCategoryId, attribute.Id, cancellationToken);
            if (!values.Succeeded)
                return ToAppError(values.Error!);
            var unknown = valueIds.Except(values.Value!.Select(v => v.Id)).ToList();
            if (unknown.Count > 0)
                return Result<CategoryMappingDto>.Invalid($"{attribute.Name} için geçersiz değer numarası: {string.Join(", ", unknown)}.");
        }

        mapping.SetAttributes(request.Attributes);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CategoryMappingDto>.Success(ToDto(mapping));
    }

    public async Task<IReadOnlyList<BrandMappingDto>> ListBrandMappingsAsync(CancellationToken cancellationToken = default) =>
        await brandMappings.ListAsync(q => q.Where(m => m.Marketplace == Marketplace)
            .Select(m => new BrandMappingDto(m.BrandId, m.ExternalBrandId, m.ExternalBrandName)), cancellationToken);

    public async Task<Result<BrandMappingDto>> MapBrandAsync(int brandId, MapBrandRequest request, CancellationToken cancellationToken = default)
    {
        if (!await brands.AnyAsync(b => b.Id == brandId, cancellationToken))
            return Result<BrandMappingDto>.NotFound("Marka bulunamadı.");
        if (request.ExternalBrandId <= 0)
            return Result<BrandMappingDto>.Invalid("Trendyol marka numarası geçersiz.");

        var mapping = await brandMappings.FirstOrDefaultAsync(m => m.Marketplace == Marketplace && m.BrandId == brandId, cancellationToken);
        if (mapping is null)
            brandMappings.Add(mapping = MarketplaceBrandMapping.Create(Marketplace, brandId, request.ExternalBrandId, request.ExternalBrandName));
        else
            mapping.Update(request.ExternalBrandId, request.ExternalBrandName);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BrandMappingDto>.Success(new BrandMappingDto(brandId, mapping.ExternalBrandId, mapping.ExternalBrandName));
    }

    public async Task<Result<ListingDto>> GetAsync(int productId, CancellationToken cancellationToken = default)
    {
        var listing = await FindListingAsync(productId, cancellationToken);
        return listing is null ? Result<ListingDto>.NotFound("Ürün Trendyol'a gönderilmemiş.") : Result<ListingDto>.Success(ToDto(listing));
    }

    public async Task<Result<ListingDto>> PublishAsync(int productId, PublishListingRequest request, CancellationToken cancellationToken = default)
    {
        var product = await products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
            return Result<ListingDto>.NotFound("Ürün bulunamadı.");

        var categoryMapping = await categoryMappings.FirstOrDefaultAsync(m => m.Marketplace == Marketplace && m.CategoryId == product.CategoryId, cancellationToken);
        var brandMapping = await brandMappings.FirstOrDefaultAsync(m => m.Marketplace == Marketplace && m.BrandId == product.BrandId, cancellationToken);
        if (categoryMapping is null || brandMapping is null)
            return Result<ListingDto>.Invalid("Ürünün kategorisi ve markası önce Trendyol ile eşleştirilmelidir.");

        var images = product.Images.OrderBy(i => i.SortOrder).Select(i => media.ToPublicUrl(i.Path)).ToList();
        if (images.Count == 0)
            return Result<ListingDto>.Invalid("Trendyol'a göndermek için en az bir görsel gerekir.");
        if (images.Any(url => !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            return Result<ListingDto>.Invalid("Görsel adresleri herkese açık https olmalıdır; Media:PublicBaseUrl ayarını kontrol edin.");

        var listing = await FindListingAsync(productId, cancellationToken);
        if (listing is { IsSyncable: true })
            return Result<ListingDto>.Conflict("Ürün Trendyol'da zaten kabul edildi; stok ve fiyat otomatik güncelleniyor.");

        // Talepteki özellikler kategori eşlemesinden çözülenleri aynı özellik numarasında ezer.
        var requestVariantAttributes = (request.VariantAttributes ?? []).ToDictionary(v => v.VariantId, v => v.Attributes);
        var productAttributes = Merge(categoryMapping.ResolveProductAttributes(), request.ProductAttributes ?? []);
        var activeVariants = product.Variants.Where(v => v.IsActive).ToList();
        var variantAttributes = activeVariants
            .Select(v => new VariantListingAttributes(v.Id, Merge(categoryMapping.ResolveVariantAttributes(v.Attributes), requestVariantAttributes.GetValueOrDefault(v.Id) ?? [])))
            .ToList();

        var missing = await MissingRequiredAttributesAsync(categoryMapping.ExternalCategoryId, productAttributes, variantAttributes, cancellationToken);
        if (missing.Count > 0)
            return Result<ListingDto>.Invalid("Trendyol kategorisinin zorunlu özellikleri eksik: " + string.Join(", ", missing) +
                ". Kategori özellik eşlemesini tamamlayın veya istekte gönderin.");

        var items = activeVariants.Select(v => new CreateProductItem
        {
            Barcode = v.Barcode,
            Title = product.Name,
            ProductMainId = product.ProductCode,
            BrandId = brandMapping.ExternalBrandId,
            CategoryId = categoryMapping.ExternalCategoryId,
            Quantity = Math.Min(v.StockQuantity, 20000),
            StockCode = v.Sku,
            Description = string.IsNullOrWhiteSpace(product.Description) ? product.Name : product.Description,
            ListPrice = v.ListPrice ?? v.Price,
            SalePrice = v.Price,
            VatRate = product.VatRate,
            Images = images.Select(url => new TrendyolImage(url)).ToList(),
            Attributes = productAttributes
                .Concat(variantAttributes.Single(a => a.VariantId == v.Id).Attributes)
                .Select(a => new TrendyolProductAttribute(a.AttributeId, a.ValueId, a.CustomValue))
                .ToList(),
            DeliveryOption = request.DeliveryDuration is null ? null : new TrendyolDeliveryOption(request.DeliveryDuration)
        }).ToList();

        var createRequest = new CreateTrendyolProductsRequest(items);
        var validation = await new CreateTrendyolProductsRequestValidator().ValidateAsync(createRequest, cancellationToken);
        if (!validation.IsValid)
            return Result<ListingDto>.Invalid("Ürün Trendyol kurallarına uymuyor: " + string.Join("; ", validation.Errors.Select(e => e.ErrorMessage).Distinct()));

        var response = await trendyolProducts.CreateAsync(createRequest, cancellationToken);
        if (!response.Succeeded)
        {
            var error = ToAppError(response.Error!);
            logs.Add(MarketplaceSyncLog.Write(Marketplace, SyncOperation.Publish, productId.ToString(), false, error.Message, Now));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return error;
        }

        if (listing is null)
            listings.Add(listing = MarketplaceListing.Create(Marketplace, productId));
        listing.SetContent(productAttributes, variantAttributes, request.DeliveryDuration);
        listing.MarkSubmitted(response.Value!.BatchRequestId, Now);
        logs.Add(MarketplaceSyncLog.Write(Marketplace, SyncOperation.Publish, productId.ToString(), true,
            $"{items.Count} varyant gönderildi (batch {response.Value.BatchRequestId}).", Now));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ListingDto>.Success(ToDto(listing));
    }

    public async Task<int> RefreshStatusesAsync(CancellationToken cancellationToken = default)
    {
        var submitted = await listings.ListAsync(l => l.Marketplace == Marketplace && l.Status == ListingStatus.Submitted, cancellationToken);
        var awaiting = await listings.ListAsync(q => q
            .Where(l => l.Marketplace == Marketplace && l.Status == ListingStatus.AwaitingApproval)
            .OrderBy(l => l.LastCheckedAt).Take(MaxListingsPerRefresh), cancellationToken);
        var changed = 0;

        var batchIds = submitted.Select(l => l.LastBatchRequestId).ToList();
        var batchById = (await batches.ListAsync(b => b.Marketplace == Marketplace && batchIds.Contains(b.BatchRequestId), cancellationToken))
            .ToDictionary(b => b.BatchRequestId);

        foreach (var listing in submitted)
        {
            if (listing.LastBatchRequestId is null || !batchById.TryGetValue(listing.LastBatchRequestId, out var batch) || batch.IsPending)
                continue;

            var failures = batch.Failures.Select(f => $"{f.Barcode}: {string.Join(", ", f.Reasons)}").ToList();
            if (batch.Status == MarketplaceBatchStatus.Expired)
                failures.Add("Trendyol batch sonucu alınamadı; ürünü tekrar gönderin.");
            listing.ApplyBatchResult(failures.Count == 0, failures, Now);
            changed++;
        }

        foreach (var listing in awaiting)
        {
            var barcodes = await BarcodesAsync(listing.ProductId, cancellationToken);
            if (barcodes.Count == 0)
                continue;

            var approved = await trendyolProducts.GetApprovedAsync(new ApprovedProductQuery { Barcodes = barcodes, Size = 50 }, cancellationToken);
            if (approved.Succeeded && approved.Value!.Content is { Count: > 0 } content)
            {
                listing.MarkLive(content[0].ContentId, Now);
                changed++;
                continue;
            }

            var unapproved = await trendyolProducts.GetUnapprovedAsync(new UnapprovedProductQuery { Barcodes = barcodes, Size = 50 }, cancellationToken);
            var rejected = unapproved.Succeeded ? (unapproved.Value!.Content ?? []).Where(p => p.Status == "rejected").ToList() : [];
            if (rejected.Count > 0)
            {
                listing.MarkRejected(string.Join("; ", rejected.Select(p =>
                    $"{p.Barcode}: {string.Join(", ", (p.RejectReasonDetails ?? []).Select(r => $"{r.RejectReason} - {r.RejectReasonDetail}"))}")), Now);
                changed++;
            }
        }

        if (changed > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);
        return changed;
    }

    private async Task<List<string>> BarcodesAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        return product?.Variants.Where(v => v.IsActive).Select(v => v.Barcode).Take(50).ToList() ?? [];
    }

    private static List<ListingAttribute> Merge(IEnumerable<ListingAttribute> resolved, IEnumerable<ListingAttribute> overrides)
    {
        var result = resolved.ToDictionary(a => a.AttributeId);
        foreach (var attribute in overrides)
            result[attribute.AttributeId] = attribute;
        return result.Values.ToList();
    }

    // Özellik listesi alınamazsa kontrol atlanır; Trendyol batch sonucunda yine reddeder.
    private async Task<List<string>> MissingRequiredAttributesAsync(
        long externalCategoryId, IReadOnlyList<ListingAttribute> productAttributes, IReadOnlyList<VariantListingAttributes> variantAttributes, CancellationToken cancellationToken)
    {
        var attributes = await trendyolCatalog.GetCategoryAttributesAsync(externalCategoryId, cancellationToken);
        if (attributes is not { Succeeded: true, Value: { } list })
            return [];

        var productIds = productAttributes.Select(a => a.AttributeId).ToHashSet();
        return list
            .Where(a => a.Required && !productIds.Contains(a.Id) && (variantAttributes.Count == 0 || variantAttributes.Any(v => v.Attributes.All(x => x.AttributeId != a.Id))))
            .Select(a => a.Name)
            .ToList();
    }

    private static CategoryMappingDto ToDto(MarketplaceCategoryMapping m) => new(m.CategoryId, m.ExternalCategoryId, m.ExternalCategoryName, m.Attributes);

    private Task<MarketplaceListing?> FindListingAsync(int productId, CancellationToken cancellationToken) =>
        listings.FirstOrDefaultAsync(l => l.Marketplace == Marketplace && l.ProductId == productId, cancellationToken);

    private static IEnumerable<CategoryNodeDto> Flatten(IEnumerable<CategoryNodeDto> nodes) =>
        nodes.SelectMany(n => new[] { n }.Concat(Flatten(n.SubCategories)));

    internal static AppError ToAppError(MarketplaceError error)
    {
        var message = "Trendyol: " + (error.Messages.Count > 0 ? string.Join("; ", error.Messages) : error.Kind.ToString());
        return error.Kind switch
        {
            MarketplaceErrorKind.Validation => Errors.Invalid(message),
            MarketplaceErrorKind.NotFound => Errors.NotFound(message),
            MarketplaceErrorKind.Conflict => Errors.Conflict(message),
            _ => new AppError(ErrorKind.Unavailable, message)
        };
    }

    private static ListingDto ToDto(MarketplaceListing l) =>
        new(l.ProductId, l.Status, l.ExternalContentId, l.LastBatchRequestId, l.StatusMessage, l.LastSubmittedAt, l.LastCheckedAt,
            l.ProductAttributes, l.VariantAttributes, l.DeliveryDuration);
}

public class PublishListingRequestValidator : AbstractValidator<PublishListingRequest>
{
    public PublishListingRequestValidator()
    {
        RuleForEach(x => x.ProductAttributes).Must(a => a.AttributeId > 0 && (a.ValueId is not null || !string.IsNullOrWhiteSpace(a.CustomValue)))
            .WithMessage("Her özellik için değer numarası veya serbest değer girilmelidir.");
        RuleFor(x => x.DeliveryDuration).InclusiveBetween(1, 30).When(x => x.DeliveryDuration is not null);
    }
}
