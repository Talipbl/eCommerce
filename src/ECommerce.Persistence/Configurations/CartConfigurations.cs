using ECommerce.Customers;
using ECommerce.Shopping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> entity)
    {
        entity.ToTable("Shopping_Carts", "dbo");
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.CustomerId).IsUnique().HasFilter(CatalogTables.NotDeleted);
        entity.HasOne<Customer>().WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.Items).WithOne().HasForeignKey(i => i.CartId).OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(e => e.Items).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> entity)
    {
        entity.ToTable("Shopping_CartItems", "dbo");
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.CartId, e.VariantId });
    }
}
