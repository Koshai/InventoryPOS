using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class GoodsReceiptConfiguration
    : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(
        EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.ToTable("GoodsReceipts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReceiptNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ReceivedAt)
            .IsRequired();

        builder.Property(x => x.SupplierInvoiceNumber)
            .HasMaxLength(100);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasOne(x => x.PurchaseOrder)
            .WithMany()
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.GoodsReceipt)
            .HasForeignKey(x => x.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ReceiptNumber)
            .IsUnique();

        builder.HasIndex(x => x.PurchaseOrderId);

        builder.HasIndex(x => x.SupplierId);

        builder.HasIndex(x => x.ReceivedAt);
    }
}