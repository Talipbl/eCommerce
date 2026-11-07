using ECommerce.Catalog;
using ECommerce.Marketplaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ECommerce.Persistence;

/// <summary>
/// Varyantın fiyat, stok veya aktiflik değişikliklerini aynı kayıt içinde stok/fiyat outbox'ına yazar.
/// </summary>
public class InventoryOutboxInterceptor : SaveChangesInterceptor
{
    private static readonly string[] TrackedProperties =
        [nameof(ProductVariant.Price), nameof(ProductVariant.ListPrice), nameof(ProductVariant.StockQuantity), nameof(ProductVariant.IsActive)];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Record(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Record(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Record(DbContext? context)
    {
        if (context is null)
            return;

        var changed = context.ChangeTracker.Entries<ProductVariant>()
            .Where(e => e.State == EntityState.Modified && TrackedProperties.Any(p => e.Property(p).IsModified))
            .Select(e => e.Entity.Id)
            .Distinct()
            .ToList();

        foreach (var variantId in changed)
            context.Add(MarketplaceInventoryChange.For(variantId));
    }
}
