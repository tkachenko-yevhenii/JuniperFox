using JuniperFox.Domain;
using JuniperFox.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JuniperFox.Infrastructure.Persistence.Configurations;

internal sealed class ProductListShareConfiguration : IEntityTypeConfiguration<ProductListShare>
{
    public void Configure(EntityTypeBuilder<ProductListShare> builder)
    {
        builder.ToTable("product_list_shares");

        builder.HasKey(s => new { s.ProductListId, s.UserId });

        builder.Property(s => s.SharedAtUtc)
            .IsRequired();

        builder.HasOne<ProductList>()
            .WithMany(l => l.Shares)
            .HasForeignKey(s => s.ProductListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
