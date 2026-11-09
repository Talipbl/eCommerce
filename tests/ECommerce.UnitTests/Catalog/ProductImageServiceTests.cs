using ECommerce.Application.Catalog;
using ECommerce.Application.Common;
using ECommerce.Catalog;

namespace ECommerce.UnitTests.Catalog;

public sealed class ProductImageServiceTests : IAsyncLifetime
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    private readonly CatalogTestDb _db = new();
    private readonly InMemoryFileStorage _storage = new();
    private Product _product = null!;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ProductImageService Service => new(_db.Context, _storage, _db.ProductService());

    public async ValueTask InitializeAsync()
    {
        var brand = await _db.AddAsync(Brand.Create("Acme", "acme"));
        var category = await _db.AddAsync(Category.Create("Tişört", "tisort", null, null, 0));
        _product = await _db.AddProductAsync("Ürün", brand, category);
    }

    [Fact]
    public async Task Jpeg_upload_is_stored_under_product_folder()
    {
        var result = await Service.UploadAsync(_product.Id, new MemoryStream(Jpeg), "image/jpeg", Jpeg.Length, "Ön", Ct);

        var image = result.Value!.Images.Last();
        image.AltText.ShouldBe("Ön");
        var stored = _storage.Files.Keys.ShouldHaveSingleItem();
        stored.ShouldStartWith($"products/{_product.Id}/");
        stored.ShouldEndWith(".jpg");
        image.Url.ShouldBe($"https://cdn.test/{stored}");
    }

    [Fact]
    public async Task Content_must_match_an_allowed_image_type()
    {
        var spoofed = await Service.UploadAsync(_product.Id, new MemoryStream("<?php echo 1;"u8.ToArray()), "image/jpeg", 13, null, Ct);
        var wrongType = await Service.UploadAsync(_product.Id, new MemoryStream(Png), "image/gif", Png.Length, null, Ct);

        spoofed.Error!.Kind.ShouldBe(ErrorKind.Validation);
        wrongType.Error!.Kind.ShouldBe(ErrorKind.Validation);
        _storage.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task Files_larger_than_limit_are_rejected()
    {
        var result = await Service.UploadAsync(_product.Id, new MemoryStream(Jpeg), "image/jpeg", ProductImageService.MaxFileSize + 1, null, Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.Validation);
    }

    [Fact]
    public async Task Unknown_product_is_not_found()
    {
        var result = await Service.UploadAsync(999, new MemoryStream(Png), "image/png", Png.Length, null, Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Deleting_an_image_removes_the_file()
    {
        var uploaded = (await Service.UploadAsync(_product.Id, new MemoryStream(Png), "image/png", Png.Length, null, Ct)).Value!;
        var imageId = uploaded.Images.Last().Id;

        var result = await Service.DeleteAsync(_product.Id, imageId, Ct);

        result.Value!.Images.ShouldNotContain(i => i.Id == imageId);
        _storage.Files.ShouldBeEmpty();
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private sealed class InMemoryFileStorage : IFileStorage
    {
        public Dictionary<string, byte[]> Files { get; } = [];

        public async Task<string> SaveAsync(string directory, string extension, Stream content, CancellationToken cancellationToken)
        {
            var path = $"{directory}/{Guid.NewGuid():N}{extension}";
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            Files[path] = buffer.ToArray();
            return path;
        }

        public Task DeleteAsync(string path, CancellationToken cancellationToken)
        {
            Files.Remove(path);
            return Task.CompletedTask;
        }
    }
}
