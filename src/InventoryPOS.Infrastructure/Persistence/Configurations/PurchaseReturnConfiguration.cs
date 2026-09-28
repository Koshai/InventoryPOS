using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class PurchaseReturnConfiguration
    : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(
        EntityTypeBuilder<PurchaseReturn> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReturnNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.ReturnNumber)
            .IsUnique();

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Reason)
            .HasMaxLength(500);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasOne(x => x.GoodsReceipt)
            .WithMany(x => x.Returns)
            .HasForeignKey(x => x.GoodsReceiptId)
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
            .WithOne(x => x.PurchaseReturn)
            .HasForeignKey(x => x.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}