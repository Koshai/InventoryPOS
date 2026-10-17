using InventoryPOS.Application.Licensing;
using System.Security.Cryptography;
using InventoryPOS.Application.DTOs.Documents;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InventoryPOS.Infrastructure.Services;

public sealed class DocumentIdentifierService
    : IDocumentIdentifierService
{
    private const string PayloadPrefix = "IPOS";
    private const int PayloadVersion = 1;
    private const int LookupTokenLength = 32;

    private readonly InventoryPosDbContext _db;

    private readonly ILicenseAccessGuard _licenseAccessGuard;


    public DocumentIdentifierService(
        InventoryPosDbContext db,
        ILicenseAccessGuard licenseAccessGuard)
    {
        _licenseAccessGuard = licenseAccessGuard ?? throw new ArgumentNullException(nameof(licenseAccessGuard));
        _db = db;
    }

    public async Task<DocumentIdentifierDto> GetOrCreateAsync(
    DocumentType documentType,
    Guid documentId,
    CancellationToken cancellationToken = default)
    {
        _licenseAccessGuard.EnsureCanWrite();

        ValidateDocumentType(documentType);

        if (documentId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A valid document ID is required.");
        }

        // ---------------------------------------------------------
        // Existing canonical identifier
        // ---------------------------------------------------------

        var existing =
            await _db.DocumentIdentifiers
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.DocumentType == documentType &&
                        x.DocumentId == documentId,
                    cancellationToken);

        if (existing is not null)
        {
            if (existing.RevokedAtUtc.HasValue)
            {
                throw new InvalidOperationException(
                    "The document identifier has been revoked.");
            }

            return Map(existing);
        }

        // ---------------------------------------------------------
        // Verify actual document exists
        // ---------------------------------------------------------

        if (!await DocumentExistsAsync(
                documentType,
                documentId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "The requested document does not exist.");
        }

        // ---------------------------------------------------------
        // Create canonical identifier.
        //
        // Retry only handles the extraordinarily unlikely case
        // where a randomly generated LookupToken already exists.
        // ---------------------------------------------------------

        const int maxTokenAttempts = 5;

        for (var attempt = 1;
             attempt <= maxTokenAttempts;
             attempt++)
        {
            var identifier =
                new DocumentIdentifier
                {
                    DocumentType =
                        documentType,

                    DocumentId =
                        documentId,

                    LookupToken =
                        CreateLookupToken()
                };

            _db.DocumentIdentifiers.Add(
                identifier);

            try
            {
                await _db.SaveChangesAsync(
                    cancellationToken);

                return Map(identifier);
            }
            catch (DbUpdateException ex)
                when (IsUniqueViolation(ex))
            {
                // A failed Added entity must not remain tracked
                // in the desktop DbContext.
                _db.Entry(identifier).State =
                    EntityState.Detached;

                // -------------------------------------------------
                // First possibility:
                //
                // another caller created the canonical identifier
                // for this exact document.
                // -------------------------------------------------

                var concurrent =
                    await _db.DocumentIdentifiers
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            x =>
                                x.DocumentType ==
                                    documentType &&
                                x.DocumentId ==
                                    documentId,
                            cancellationToken);

                if (concurrent is not null)
                {
                    if (concurrent.RevokedAtUtc.HasValue)
                    {
                        throw new InvalidOperationException(
                            "The document identifier has been revoked.");
                    }

                    return Map(concurrent);
                }

                // -------------------------------------------------
                // Second possibility:
                //
                // LookupToken itself collided with another row.
                //
                // Generate a fresh cryptographically random token
                // and retry.
                // -------------------------------------------------

                if (attempt ==
                    maxTokenAttempts)
                {
                    throw new InvalidOperationException(
                        "A unique document identifier could not " +
                        "be generated after multiple attempts.",
                        ex);
                }
            }
        }

        // Defensive only. The loop either returns or throws.
        throw new InvalidOperationException(
            "A document identifier could not be generated.");
    }

    public async Task<DocumentIdentifierDto?> ResolveTokenAsync(
        string lookupToken,
        CancellationToken cancellationToken = default)
    {
        var token =
            lookupToken?.Trim();

        if (!IsValidLookupToken(token))
        {
            return null;
        }

        var identifier =
            await _db.DocumentIdentifiers
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.LookupToken == token &&
                        x.RevokedAtUtc == null,
                    cancellationToken);

        return identifier is null
            ? null
            : Map(identifier);
    }

    public async Task<DocumentIdentifierDto?> ResolvePayloadAsync(
        string qrPayload,
        CancellationToken cancellationToken = default)
    {
        if (!TryParsePayload(
                qrPayload,
                out var token))
        {
            return null;
        }

        return await ResolveTokenAsync(
            token,
            cancellationToken);
    }

    public async Task RevokeAsync(
        DocumentType documentType,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        _licenseAccessGuard.EnsureCanWrite();

        ValidateDocumentType(documentType);

        if (documentId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A valid document ID is required.");
        }

        var identifier =
            await _db.DocumentIdentifiers
                .SingleOrDefaultAsync(
                    x =>
                        x.DocumentType == documentType &&
                        x.DocumentId == documentId,
                    cancellationToken);

        if (identifier is null)
        {
            return;
        }

        if (identifier.RevokedAtUtc.HasValue)
        {
            return;
        }

        identifier.RevokedAtUtc =
            DateTime.UtcNow;

        await _db.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<bool> DocumentExistsAsync(
        DocumentType documentType,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return documentType switch
        {
            DocumentType.SaleInvoice =>
                await _db.Sales
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.Id == documentId,
                        cancellationToken),

            DocumentType.PurchaseOrder =>
                await _db.PurchaseOrders
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.Id == documentId,
                        cancellationToken),

            DocumentType.GoodsReceipt =>
                await _db.GoodsReceipts
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.Id == documentId,
                        cancellationToken),

            DocumentType.PurchaseReturn =>
                await _db.PurchaseReturns
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.Id == documentId,
                        cancellationToken),

            DocumentType.PurchaseRequisition =>
                await _db.PurchaseRequisitions
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.Id == documentId,
                        cancellationToken),

            DocumentType.SalesQuote =>
                await _db.SalesQuotes
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.Id == documentId,
                        cancellationToken),

            DocumentType.WarrantyClaim =>
                await _db.WarrantyClaims
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.Id == documentId,
                        cancellationToken),

            _ => false
        };
    }

    private static DocumentIdentifierDto Map(
        DocumentIdentifier identifier)
    {
        return new DocumentIdentifierDto
        {
            DocumentType =
                identifier.DocumentType,

            DocumentId =
                identifier.DocumentId,

            LookupToken =
                identifier.LookupToken,

            QrPayload =
                BuildPayload(
                    identifier.LookupToken)
        };
    }

    private static string BuildPayload(
        string token)
    {
        return
            $"{PayloadPrefix}:" +
            $"{PayloadVersion}:" +
            $"{token}";
    }

    private static bool TryParsePayload(
        string? payload,
        out string token)
    {
        token =
            string.Empty;

        var text =
            payload?.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts =
            text.Split(
                ':',
                StringSplitOptions.None);

        if (parts.Length != 3)
        {
            return false;
        }

        if (!string.Equals(
                parts[0],
                PayloadPrefix,
                StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(
                parts[1],
                out var version) ||
            version != PayloadVersion)
        {
            return false;
        }

        if (!IsValidLookupToken(parts[2]))
        {
            return false;
        }

        token =
            parts[2];

        return true;
    }

    private static string CreateLookupToken()
    {
        // 24 random bytes = 192 bits of entropy.
        //
        // Base64Url gives us a compact printable/scannable token
        // without '+', '/', or '='.
        var bytes =
            RandomNumberGenerator.GetBytes(24);

        return Convert
            .ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static void ValidateDocumentType(
        DocumentType documentType)
    {
        if (!Enum.IsDefined(documentType))
        {
            throw new InvalidOperationException(
                "Invalid document type.");
        }
    }

    private static bool IsUniqueViolation(
    DbUpdateException exception)
    {
        return exception.InnerException
            is PostgresException postgres &&
            postgres.SqlState ==
                PostgresErrorCodes.UniqueViolation;
    }

    private static bool IsValidLookupToken(
    string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (token.Length != LookupTokenLength)
        {
            return false;
        }

        foreach (var character in token)
        {
            var valid =
                character >= 'A' &&
                character <= 'Z' ||

                character >= 'a' &&
                character <= 'z' ||

                character >= '0' &&
                character <= '9' ||

                character == '-' ||

                character == '_';

            if (!valid)
            {
                return false;
            }
        }

        return true;
    }
}