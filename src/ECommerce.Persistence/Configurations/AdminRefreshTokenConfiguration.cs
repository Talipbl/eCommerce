using ECommerce.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations;

public class AdminRefreshTokenConfiguration : IEntityTypeConfiguration<AdminRefreshToken>
{
    public void Configure(EntityTypeBuilder<AdminRefreshToken> entity)
    {
        entity.HasKey(e => e.Id);

        entity.ToTable("Def_AdminRefreshTokens", "dbo");

        entity.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
        entity.Property(e => e.ReplacedByTokenHash).HasMaxLength(64);
        entity.HasIndex(e => e.TokenHash).IsUnique();

        entity.HasOne(e => e.Admin)
            .WithMany(a => a.RefreshTokens)
            .HasForeignKey(e => e.AdminId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
