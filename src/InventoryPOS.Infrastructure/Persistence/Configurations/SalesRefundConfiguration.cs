using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class SalesRefundConfiguration :
    IEntityTypeConfiguration<SalesRefund>
{
    public void Configure(
        EntityTypeBuilder<SalesRefund> builder)
    {
        builder.ToTable("SalesRefunds");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.ReferenceNumber)
            .HasMaxLength(200);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasIndex(x => x.SalesReturnId);

        builder.HasIndex(x => x.RefundDate);

        builder.HasIndex(x => x.RefundType);

        builder.HasOne(x => x.SalesReturn)
            .WithMany(x => x.Refunds)
            .HasForeignKey(x => x.SalesReturnId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}