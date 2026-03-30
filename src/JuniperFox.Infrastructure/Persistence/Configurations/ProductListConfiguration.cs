using JuniperFox.Domain;
using JuniperFox.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JuniperFox.Infrastructure.Persistence.Configurations;

internal sealed class ProductListConfiguration : IEntityTypeConfiguration<ProductList>
{
    public void Configure(EntityTypeBuilder<ProductList> builder)
    {
        builder.ToTable("product_lists");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(l => l.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(l => l.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(l => l.Items)
            .WithOne(i => i.ProductList)
            .HasForeignKey(i => i.ProductListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.Shares)
            .WithOne()
            .HasForeignKey(s => s.ProductListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
