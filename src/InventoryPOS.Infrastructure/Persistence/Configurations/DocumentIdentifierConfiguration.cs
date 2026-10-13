using InventoryPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryPOS.Infrastructure.Persistence.Configurations;

public sealed class DocumentIdentifierConfiguration
    : IEntityTypeConfiguration<DocumentIdentifier>
{
    public void Configure(
        EntityTypeBuilder<DocumentIdentifier> builder)
    {
        builder.ToTable("DocumentIdentifiers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.DocumentId)
            .IsRequired();

        builder.Property(x => x.LookupToken)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.RevokedAtUtc);

        builder.HasIndex(x => x.LookupToken)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.DocumentType,
            x.DocumentId
        })
        .IsUnique();

        builder.HasIndex(x => x.RevokedAtUtc);
    }
}