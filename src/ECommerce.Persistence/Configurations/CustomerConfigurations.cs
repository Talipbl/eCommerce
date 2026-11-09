using ECommerce.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> entity)
    {
        entity.ToTable("Customers", "dbo");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Email).HasMaxLength(150).IsRequired();
        entity.Property(e => e.FirstName).HasMaxLength(50).IsRequired();
        entity.Property(e => e.LastName).HasMaxLength(50).IsRequired();
        entity.Property(e => e.Phone).HasMaxLength(20);
        entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
        entity.HasIndex(e => e.Email).IsUnique().HasFilter(CatalogTables.NotDeleted);

        entity.HasMany(e => e.Addresses).WithOne().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(e => e.Addresses).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}

public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> entity)
    {
        entity.ToTable("Customer_Addresses", "dbo");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Title).HasMaxLength(50).IsRequired();
        entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
        entity.Property(e => e.Phone).HasMaxLength(20).IsRequired();
        entity.Property(e => e.City).HasMaxLength(50).IsRequired();
        entity.Property(e => e.District).HasMaxLength(50).IsRequired();
        entity.Property(e => e.AddressLine).HasMaxLength(300).IsRequired();
        entity.Property(e => e.PostalCode).HasMaxLength(10);
    }
}

public class CustomerRefreshTokenConfiguration : IEntityTypeConfiguration<CustomerRefreshToken>
{
    public void Configure(EntityTypeBuilder<CustomerRefreshToken> entity)
    {
        entity.ToTable("Customer_RefreshTokens", "dbo");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
        entity.Property(e => e.ReplacedByTokenHash).HasMaxLength(64);
        entity.HasIndex(e => e.TokenHash).IsUnique();
        entity.HasOne<Customer>().WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
