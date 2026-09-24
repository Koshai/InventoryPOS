using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class SalePaymentConfiguration :
    IEntityTypeConfiguration<SalePayment>
{
    public void Configure(
        EntityTypeBuilder<SalePayment> builder)
    {
        builder.ToTable("SalePayments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.ReferenceNumber)
            .HasMaxLength(200);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasIndex(x => x.SaleId);

        builder.HasIndex(x => x.PaymentDate);

        builder.HasOne(x => x.Sale)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}