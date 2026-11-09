using ECommerce.Application.Catalog;
using ECommerce.Application.Common;
using ECommerce.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.UnitTests.Catalog;

public sealed class CategoryAndBrandServiceTests : IAsyncDisposable
{
    private readonly CatalogTestDb _db = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Category_slug_is_generated_from_name_and_must_be_unique()
    {
        var service = _db.CategoryService();

        var created = await service.CreateAsync(new SaveCategoryRequest("Kadın Giyim", null, null, null, 0, true), Ct);
        var duplicate = await service.CreateAsync(new SaveCategoryRequest("Kadın  Giyim", null, null, null, 0, true), Ct);

        created.Value!.Slug.ShouldBe("kadin-giyim");
        duplicate.Error!.Kind.ShouldBe(ErrorKind.Conflict);
    }

    [Fact]
    public async Task Unknown_parent_is_rejected()
    {
        var result = await _db.CategoryService().CreateAsync(new SaveCategoryRequest("Tişört", null, 999, null, 0, true), Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.Validation);
    }

    [Fact]
    public async Task Category_cannot_move_under_its_descendant()
    {
        var service = _db.CategoryService();
        var root = (await service.CreateAsync(new SaveCategoryRequest("Giyim", null, null, null, 0, true), Ct)).Value!;
        var child = (await service.CreateAsync(new SaveCategoryRequest("Kadın", null, root.Id, null, 0, true), Ct)).Value!;
        var grandChild = (await service.CreateAsync(new SaveCategoryRequest("Tişört", null, child.Id, null, 0, true), Ct)).Value!;

        var result = await service.UpdateAsync(root.Id, new SaveCategoryRequest("Giyim", "giyim", grandChild.Id, null, 0, true), Ct);

        result.Error!.Kind.ShouldBe(ErrorKind.Validation);
    }

    [Fact]
    public async Task Tree_is_nested_and_sorted()
    {
        var service = _db.CategoryService();
        var root = (await service.CreateAsync(new SaveCategoryRequest("Giyim", null, null, null, 0, true), Ct)).Value!;
        await service.CreateAsync(new SaveCategoryRequest("Erkek", null, root.Id, null, 2, true), Ct);
        await service.CreateAsync(new SaveCategoryRequest("Kadın", null, root.Id, null, 1, true), Ct);

        var tree = await service.GetTreeAsync(Ct);

        tree.ShouldHaveSingleItem().Children.Select(c => c.Name).ShouldBe(["Kadın", "Erkek"]);
    }

    [Fact]
    public async Task Category_with_children_or_products_cannot_be_deleted()
    {
        var service = _db.CategoryService();
        var root = (await service.CreateAsync(new SaveCategoryRequest("Giyim", null, null, null, 0, true), Ct)).Value!;
        var leaf = (await service.CreateAsync(new SaveCategoryRequest("Tişört", null, root.Id, null, 0, true), Ct)).Value!;
        var brand = await _db.AddAsync(Brand.Create("Acme", "acme"));
        await _db.AddProductAsync("Basic Tişört", brand, await _db.Context.Categories.SingleAsync(c => c.Id == leaf.Id, Ct));

        (await service.DeleteAsync(root.Id, Ct)).Error!.Kind.ShouldBe(ErrorKind.Conflict);
        (await service.DeleteAsync(leaf.Id, Ct)).Error!.Kind.ShouldBe(ErrorKind.Conflict);
    }

    [Fact]
    public async Task Empty_leaf_category_is_soft_deleted()
    {
        var service = _db.CategoryService();
        var leaf = (await service.CreateAsync(new SaveCategoryRequest("Tişört", null, null, null, 0, true), Ct)).Value!;

        (await service.DeleteAsync(leaf.Id, Ct)).Succeeded.ShouldBeTrue();

        (await service.GetTreeAsync(Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Brand_slug_must_be_unique_and_brand_in_use_cannot_be_deleted()
    {
        var service = _db.BrandService();
        var brand = (await service.CreateAsync(new SaveBrandRequest("Acme", null, true), Ct)).Value!;
        (await service.CreateAsync(new SaveBrandRequest("ACME", null, true), Ct)).Error!.Kind.ShouldBe(ErrorKind.Conflict);

        var category = await _db.AddAsync(Category.Create("Giyim", "giyim", null, null, 0));
        await _db.AddProductAsync("Basic Tişört", await _db.Context.Brands.SingleAsync(b => b.Id == brand.Id, Ct), category);

        (await service.DeleteAsync(brand.Id, Ct)).Error!.Kind.ShouldBe(ErrorKind.Conflict);
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
