using ECommerce.Catalog;
using ECommerce.Persistence;
using Microsoft.Extensions.Time.Testing;

namespace ECommerce.UnitTests.Persistence;

public class EfRepositoryTests
{
    private readonly FakeTimeProvider _clock = new(DateTimeOffset.UtcNow);

    [Fact]
    public async Task Add_and_save_then_get_by_id()
    {
        await using var context = PersistenceTestContext.Create(_clock);
        var repository = new EfRepository<Brand>(context);

        var brand = Brand.Create("Acme", "acme");
        repository.Add(brand);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var found = await repository.GetByIdAsync(brand.Id, TestContext.Current.CancellationToken);
        found.ShouldNotBeNull().Name.ShouldBe("Acme");
    }

    [Fact]
    public async Task List_count_and_any_apply_predicate()
    {
        await using var context = PersistenceTestContext.Create(_clock);
        var repository = new EfRepository<Brand>(context);
        repository.Add(Brand.Create("Acme", "acme"));
        repository.Add(Brand.Create("Globex", "globex"));
        repository.Add(Brand.Create("Acme Outdoor", "acme-outdoor"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ct = TestContext.Current.CancellationToken;
        (await repository.ListAsync(b => b.Name.StartsWith("Acme"), ct)).Count.ShouldBe(2);
        (await repository.CountAsync(cancellationToken: ct)).ShouldBe(3);
        (await repository.AnyAsync(b => b.Name == "Globex", ct)).ShouldBeTrue();
        (await repository.FirstOrDefaultAsync(b => b.Name == "Initech", ct)).ShouldBeNull();
    }
}
