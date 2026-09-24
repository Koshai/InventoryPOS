using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class GoodsReceiptLineConfiguration
    : IEntityTypeConfiguration<GoodsReceiptLine>
{
    public void Configure(
        EntityTypeBuilder<GoodsReceiptLine> builder)
    {
        builder.ToTable("GoodsReceiptLines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReceivedQuantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Ignore(x => x.LineTotal);

        builder.HasOne(x => x.PurchaseOrderLine)
            .WithMany()
            .HasForeignKey(x => x.PurchaseOrderLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.GoodsReceiptId);

        builder.HasIndex(x => x.PurchaseOrderLineId);

        builder.HasIndex(x => x.ProductVariantId);
        builder.Property(x => x.SupplierBatchNumber)
            .HasMaxLength(100);

        builder.Property(x => x.WarrantyMonths);
    }
}