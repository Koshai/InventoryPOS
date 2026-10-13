using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class SalesQuoteConfiguration
    : IEntityTypeConfiguration<SalesQuote>
{
    public void Configure(
        EntityTypeBuilder<SalesQuote> builder)
    {
        builder.ToTable("SalesQuotes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.QuoteNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.QuoteNumber)
            .IsUnique();

        builder.Property(x => x.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(x => x.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.Terms)
            .HasMaxLength(2000);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Sale)
            .WithMany()
            .HasForeignKey(x => x.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.SalesQuote)
            .HasForeignKey(x => x.SalesQuoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.LineDiscountAmount)
    .HasPrecision(18, 2);

        builder.Property(x => x.OrderDiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.OrderDiscountType)
            .HasDefaultValue(1);

        builder.Property(x => x.OrderDiscountValue)
            .HasPrecision(18, 2);

        builder.Property(x => x.SalesTaxRate)
            .HasPrecision(5, 2);

        builder.Property(x => x.TransportAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.PricingVersion)
    .IsRequired()
    .HasDefaultValue(1);
    }
}