using ECommerce.Core.Domain;

namespace ECommerce.Catalog;

public sealed record VariantAttribute(string Name, string Value);

/// <summary>
/// Ürün aggregate'i; varyantlar (barkod, fiyat, stok) ve görseller yalnızca ürün üzerinden değiştirilir.
/// </summary>
public class Product : AggregateRoot<int>
{
    public const int MaxImages = 8;

    private readonly List<ProductVariant> _variants = [];
    private readonly List<ProductImage> _images = [];

    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public string? Description { get; private set; }

    /// <summary>Model kodu; Trendyol'da productMainId olarak kullanılır.</summary>
    public string ProductCode { get; private set; } = null!;

    public int BrandId { get; private set; }

    public int CategoryId { get; private set; }

    public int VatRate { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyList<ProductVariant> Variants => _variants;

    public IReadOnlyList<ProductImage> Images => _images;

    public static Product Create(string name, string slug, string productCode, int brandId, int categoryId, int vatRate, string? description)
    {
        var product = new Product();
        product.UpdateDetails(name, slug, productCode, brandId, categoryId, vatRate, description);
        return product;
    }

    public void UpdateDetails(string name, string slug, string productCode, int brandId, int categoryId, int vatRate, string? description)
    {
        if (!CatalogRules.VatRates.Contains(vatRate))
            throw new DomainException("KDV oranı 0, 1, 10 veya 20 olmalıdır.");
        if (brandId <= 0 || categoryId <= 0)
            throw new DomainException("Marka ve kategori seçilmelidir.");

        Name = CatalogRules.RequiredText(name, "Ürün adı", 200);
        Slug = CatalogRules.ValidSlug(slug);
        ProductCode = CatalogRules.RequiredText(productCode, "Model kodu", 40);
        BrandId = brandId;
        CategoryId = categoryId;
        VatRate = vatRate;
        Description = description;
    }

    public ProductVariant AddVariant(string sku, string barcode, decimal price, decimal? listPrice, int stockQuantity, IEnumerable<VariantAttribute> attributes)
    {
        sku = CatalogRules.RequiredText(sku, "Stok kodu", 100);
        barcode = CatalogRules.RequiredText(barcode, "Barkod", 40);
        if (_variants.Any(v => v.Sku == sku))
            throw new DomainException($"'{sku}' stok kodu bu üründe zaten var.");
        if (_variants.Any(v => v.Barcode == barcode))
            throw new DomainException($"'{barcode}' barkodu bu üründe zaten var.");

        var variant = new ProductVariant(sku, barcode, attributes.ToList());
        variant.ChangePrice(price, listPrice);
        variant.SetStock(stockQuantity);
        _variants.Add(variant);
        return variant;
    }

    public void RemoveVariant(ProductVariant variant)
    {
        if (_variants.Count == 1)
            throw new DomainException("Ürünün son varyantı silinemez.");
        EnsureActiveVariantRemains(variant);
        _variants.Remove(variant);
    }

    public void SetVariantActive(ProductVariant variant, bool isActive)
    {
        if (!isActive)
            EnsureActiveVariantRemains(variant);
        variant.IsActive = isActive;
    }

    public void Publish()
    {
        if (!_variants.Any(v => v.IsActive))
            throw new DomainException("Yayınlamak için en az bir aktif varyant gerekir.");
        IsActive = true;
    }

    public void Unpublish() => IsActive = false;

    public ProductImage AddImage(string path, string? altText)
    {
        if (_images.Count >= MaxImages)
            throw new DomainException($"Bir ürüne en fazla {MaxImages} görsel eklenebilir.");

        var image = new ProductImage(CatalogRules.RequiredText(path, "Görsel yolu", 300), altText?.Trim(), _images.Count);
        _images.Add(image);
        return image;
    }

    public void RemoveImage(ProductImage image)
    {
        _images.Remove(image);
        for (var i = 0; i < _images.Count; i++)
            _images[i].SortOrder = i;
    }

    public void ReorderImages(IReadOnlyList<int> imageIds)
    {
        if (imageIds.Count != _images.Count || imageIds.Distinct().Count() != _images.Count || !_images.All(i => imageIds.Contains(i.Id)))
            throw new DomainException("Sıralama ürünün tüm görsellerini bir kez içermelidir.");

        foreach (var image in _images)
            image.SortOrder = imageIds.ToList().IndexOf(image.Id);
    }

    private void EnsureActiveVariantRemains(ProductVariant variant)
    {
        if (IsActive && variant.IsActive && _variants.Count(v => v.IsActive) == 1)
            throw new DomainException("Yayındaki ürünün son aktif varyantı pasifleştirilemez; önce ürünü yayından kaldırın.");
    }
}

public class ProductVariant : AggregateRoot<int>
{
    private List<VariantAttribute> _attributes = [];

    private ProductVariant()
    {
    }

    internal ProductVariant(string sku, string barcode, List<VariantAttribute> attributes)
    {
        Sku = sku;
        Barcode = barcode;
        _attributes = attributes;
    }

    public int ProductId { get; private set; }

    public string Sku { get; private set; } = null!;

    public string Barcode { get; private set; } = null!;

    public decimal Price { get; private set; }

    public decimal? ListPrice { get; private set; }

    public int StockQuantity { get; private set; }

    public bool IsActive { get; internal set; } = true;

    public IReadOnlyList<VariantAttribute> Attributes => _attributes;

    public void ChangePrice(decimal price, decimal? listPrice)
    {
        if (price <= 0)
            throw new DomainException("Satış fiyatı sıfırdan büyük olmalıdır.");
        if (listPrice is not null && listPrice < price)
            throw new DomainException("Liste fiyatı satış fiyatından düşük olamaz.");
        Price = price;
        ListPrice = listPrice;
    }

    public void SetStock(int quantity)
    {
        if (quantity < 0)
            throw new DomainException("Stok negatif olamaz.");
        StockQuantity = quantity;
    }

    public void SetAttributes(IEnumerable<VariantAttribute> attributes) => _attributes = attributes.ToList();

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Adet pozitif olmalıdır.");
        if (StockQuantity < quantity)
            throw new DomainException($"'{Barcode}' için yeterli stok yok.");
        StockQuantity -= quantity;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Adet pozitif olmalıdır.");
        StockQuantity += quantity;
    }
}

public class ProductImage : AggregateRoot<int>
{
    private ProductImage()
    {
    }

    internal ProductImage(string path, string? altText, int sortOrder)
    {
        Path = path;
        AltText = altText;
        SortOrder = sortOrder;
    }

    public int ProductId { get; private set; }

    /// <summary>Depolama köküne göreli yol (ör. products/12/abc.jpg).</summary>
    public string Path { get; private set; } = null!;

    public string? AltText { get; private set; }

    public int SortOrder { get; internal set; }
}
