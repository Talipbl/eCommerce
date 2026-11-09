using ECommerce.Application.Catalog;
using ECommerce.Application.Common;
using ECommerce.Catalog;
using ECommerce.Persistence;
using ECommerce.UnitTests.Persistence;
using Microsoft.Extensions.Time.Testing;
using ECommerce.UnitTests.Common;

namespace ECommerce.UnitTests.Catalog;

public sealed class CatalogTestDb : IAsyncDisposable
{
    public ECommerceContext Context { get; } = PersistenceTestContext.Create(new FakeTimeProvider(DateTimeOffset.UtcNow));

    public EfRepository<Category> Categories => new(Context);
    public EfRepository<Brand> Brands => new(Context);
    public EfRepository<Product> Products => new(Context);
    public EfRepository<ProductVariant> Variants => new(Context);

    public IMediaUrlBuilder Media { get; } = new FakeMediaUrlBuilder();

    public CategoryAdminService CategoryService() => new(Categories, Products, Context, TestMapper.Instance);

    public BrandAdminService BrandService() => new(Brands, Products, Context, TestMapper.Instance);

    public ProductAdminService ProductService() => new(Products, Variants, Brands, Categories, Context, Media, TestMapper.Instance);

    public StorefrontService Storefront() => new(Products, Categories, Brands, Media);

    public async Task<T> AddAsync<T>(T entity) where T : class
    {
        Context.Add(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public async Task<Product> AddProductAsync(string name, Brand brand, Category category, decimal price = 100m, int stock = 5, bool publish = true)
    {
        var slug = ECommerce.Core.Text.Slug.From(name);
        var product = Product.Create(name, slug, slug.ToUpperInvariant(), brand.Id, category.Id, 20, null);
        product.AddVariant($"SKU-{slug}", $"BRK-{slug}", price, null, stock, [new VariantAttribute("Beden", "M")]);
        product.AddImage($"products/{slug}/1.jpg", name);
        if (publish)
            product.Publish();
        return await AddAsync(product);
    }

    public ValueTask DisposeAsync() => Context.DisposeAsync();

    private sealed class FakeMediaUrlBuilder : IMediaUrlBuilder
    {
        public string ToPublicUrl(string path) => $"https://cdn.test/{path}";
    }
}
