using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class PurchaseReturnLineConfiguration
    : IEntityTypeConfiguration<PurchaseReturnLine>
{
    public void Configure(
        EntityTypeBuilder<PurchaseReturnLine> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3);

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 2);

        builder.Property(x => x.LineTotal)
            .HasPrecision(18, 2);

        builder.Property(x => x.Reason)
            .HasMaxLength(500);

        builder.HasOne(x => x.GoodsReceiptLine)
            .WithMany()
            .HasForeignKey(x => x.GoodsReceiptLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InventoryBatch)
            .WithMany()
            .HasForeignKey(x => x.InventoryBatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}