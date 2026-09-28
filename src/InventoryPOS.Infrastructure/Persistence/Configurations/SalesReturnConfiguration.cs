using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class SalesReturnConfiguration :
    IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(
        EntityTypeBuilder<SalesReturn> builder)
    {
        builder.ToTable("SalesReturns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReturnNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.ReturnNumber)
            .IsUnique();

        builder.Property(x => x.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(x => x.RefundAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.CreditAdjustmentAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Reason)
            .HasMaxLength(500);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.SaleId);

        builder.HasIndex(x => x.CustomerId);

        builder.HasIndex(x => x.LocationId);

        builder.HasIndex(x => x.ReturnDate);

        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Sale)
            .WithMany(x => x.Returns)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}