using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public sealed class ProductBarcodeConfiguration
    : IEntityTypeConfiguration<ProductBarcode>
{
    public void Configure(
        EntityTypeBuilder<ProductBarcode> builder)
    {
        builder.ToTable("ProductBarcodes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Value)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IsPrimary)
            .IsRequired();

        // A barcode must identify exactly one variant.
        builder.HasIndex(x => x.Value)
            .IsUnique();

        builder.HasIndex(x => x.ProductVariantId);
        builder.HasIndex(x => x.ProductVariantId)
    .IsUnique()
    .HasFilter("\"IsPrimary\" = TRUE");

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}