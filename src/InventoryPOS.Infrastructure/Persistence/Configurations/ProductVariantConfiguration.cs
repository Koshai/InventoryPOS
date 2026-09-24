using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration :
    IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(
        EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SKU)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.SKU)
            .IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(200);

        builder.Property(x => x.NameBn)
            .HasMaxLength(250);

        builder.Property(x => x.CostPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.SellingPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.ReorderLevel)
            .HasPrecision(18, 3);
    }
}