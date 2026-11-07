using ECommerce.Admin;
using ECommerce.Catalog;
using ECommerce.Core.Domain;
using ECommerce.Core.Persistence;
using ECommerce.Customers;
using ECommerce.Marketplaces;
using ECommerce.Ordering;
using ECommerce.Shopping;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ECommerce.Persistence;

public class ECommerceContext(DbContextOptions<ECommerceContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<DefAdmin> DefAdmins => Set<DefAdmin>();

    public DbSet<AdminRefreshToken> AdminRefreshTokens => Set<AdminRefreshToken>();

    public DbSet<MarketplaceBatchRequest> MarketplaceBatchRequests => Set<MarketplaceBatchRequest>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<CustomerRefreshToken> CustomerRefreshTokens => Set<CustomerRefreshToken>();

    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<MarketplaceCategoryMapping> MarketplaceCategoryMappings => Set<MarketplaceCategoryMapping>();

    public DbSet<MarketplaceBrandMapping> MarketplaceBrandMappings => Set<MarketplaceBrandMapping>();

    public DbSet<MarketplaceListing> MarketplaceListings => Set<MarketplaceListing>();

    public DbSet<MarketplaceInventoryChange> MarketplaceInventoryChanges => Set<MarketplaceInventoryChange>();

    public DbSet<MarketplaceOrderStatusUpdate> MarketplaceOrderStatusUpdates => Set<MarketplaceOrderStatusUpdate>();

    public DbSet<MarketplaceSyncState> MarketplaceSyncStates => Set<MarketplaceSyncState>();

    public DbSet<MarketplaceSyncLog> MarketplaceSyncLogs => Set<MarketplaceSyncLog>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("root");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ECommerceContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType))
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(NotDeletedFilter(entityType.ClrType));
        }
    }

    private static LambdaExpression NotDeletedFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var deletedDate = Expression.Property(parameter, nameof(IAuditableEntity.DeletedDate));
        var isNotDeleted = Expression.Equal(deletedDate, Expression.Constant(null, typeof(DateTime?)));
        return Expression.Lambda(isNotDeleted, parameter);
    }
}
