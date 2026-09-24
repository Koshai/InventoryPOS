using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public class LocationConfiguration :
    IEntityTypeConfiguration<Location>
{
    public void Configure(
        EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.LocationType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.HasIndex(x => x.Code)
            .IsUnique();
    }
}