using JuniperFox.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JuniperFox.Infrastructure.Persistence.Configurations;

internal sealed class ProductListItemConfiguration : IEntityTypeConfiguration<ProductListItem>
{
    public void Configure(EntityTypeBuilder<ProductListItem> builder)
    {
        builder.ToTable("product_list_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(i => i.SortOrder)
            .IsRequired();

        builder.Property(i => i.IsPurchased)
            .IsRequired()
            .HasDefaultValue(false);
    }
}
