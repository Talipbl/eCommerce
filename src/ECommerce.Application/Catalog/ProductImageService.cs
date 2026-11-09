using ECommerce.Application.Common;
using ECommerce.Catalog;
using ECommerce.Core.Persistence;

namespace ECommerce.Application.Catalog;

public class ProductImageService(
    IUnitOfWork unitOfWork,
    IFileStorage storage,
    ProductAdminService productService) : IProductImageService
{
    public const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly (string ContentType, string Extension, byte[] Signature)[] AllowedTypes =
    [
        ("image/jpeg", ".jpg", [0xFF, 0xD8, 0xFF]),
        ("image/png", ".png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
        ("image/webp", ".webp", "RIFF"u8.ToArray())
    ];

    public async Task<Result<ProductDetailDto>> UploadAsync(int productId, Stream content, string contentType, long length, string? altText, CancellationToken cancellationToken = default)
    {
        if (length is <= 0 or > MaxFileSize)
            return Result<ProductDetailDto>.Invalid("Görsel 5 MB'tan büyük olamaz.");

        var type = AllowedTypes.FirstOrDefault(t => t.ContentType == contentType);
        if (type.Extension is null)
            return Result<ProductDetailDto>.Invalid("Yalnızca JPEG, PNG veya WEBP görsel yüklenebilir.");

        var product = await productService.FindAsync(productId, cancellationToken);
        if (product is null)
            return Result<ProductDetailDto>.NotFound("Ürün bulunamadı.");

        // Dosya uzantısı veya Content-Type'a güvenilmez; içerik imzası kontrol edilir.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        if (!bytes.AsSpan().StartsWith(type.Signature) || (type.Extension == ".webp" && !IsWebp(bytes)))
            return Result<ProductDetailDto>.Invalid("Dosya içeriği seçilen görsel türüyle uyuşmuyor.");

        if (product.Images.Count >= Product.MaxImages)
            return Result<ProductDetailDto>.Invalid($"Bir ürüne en fazla {Product.MaxImages} görsel eklenebilir.");

        buffer.Position = 0;
        var path = await storage.SaveAsync($"products/{productId}", type.Extension, buffer, cancellationToken);
        try
        {
            product.AddImage(path, altText);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(path, CancellationToken.None);
            throw;
        }

        return Result<ProductDetailDto>.Success(productService.ToDetail(product));
    }

    public async Task<Result<ProductDetailDto>> DeleteAsync(int productId, int imageId, CancellationToken cancellationToken = default)
    {
        var product = await productService.FindAsync(productId, cancellationToken);
        var image = product?.Images.FirstOrDefault(i => i.Id == imageId);
        if (product is null || image is null)
            return Result<ProductDetailDto>.NotFound("Görsel bulunamadı.");

        product.RemoveImage(image);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(image.Path, cancellationToken);

        return Result<ProductDetailDto>.Success(productService.ToDetail(product));
    }

    public async Task<Result<ProductDetailDto>> ReorderAsync(int productId, IReadOnlyList<int> imageIds, CancellationToken cancellationToken = default)
    {
        var product = await productService.FindAsync(productId, cancellationToken);
        if (product is null)
            return Result<ProductDetailDto>.NotFound("Ürün bulunamadı.");

        product.ReorderImages(imageIds);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ProductDetailDto>.Success(productService.ToDetail(product));
    }

    private static bool IsWebp(byte[] bytes) => bytes.Length >= 12 && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8);
}
