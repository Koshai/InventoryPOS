using InventoryPOS.Application.DTOs.Settings;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public sealed class BusinessProfileService : IBusinessProfileService
{
    private readonly InventoryPosDbContext _dbContext;

    public BusinessProfileService(
        InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BusinessProfileDto?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var entity =
            await _dbContext.BusinessProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return null;

        return Map(entity);
    }

    public async Task<BusinessProfileDto> SaveAsync(
        BusinessProfileDto profile,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profile.BusinessName))
        {
            throw new InvalidOperationException(
                "Business name is required.");
        }

        var entity =
            await _dbContext.BusinessProfiles
                .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
        {
            entity = new BusinessProfile();

            _dbContext.BusinessProfiles.Add(entity);
        }

        entity.BusinessName =
            profile.BusinessName.Trim();

        entity.BusinessNameBn =
            Clean(profile.BusinessNameBn);

        entity.Address =
            Clean(profile.Address);

        entity.AddressBn =
            Clean(profile.AddressBn);

        entity.Phone =
            Clean(profile.Phone);

        entity.Email =
            Clean(profile.Email);

        entity.Website =
            Clean(profile.Website);

        entity.BinNumber =
            Clean(profile.BinNumber);

        entity.TinNumber =
            Clean(profile.TinNumber);

        entity.TradeLicenseNumber =
            Clean(profile.TradeLicenseNumber);

        entity.CurrencyCode =
            string.IsNullOrWhiteSpace(profile.CurrencyCode)
                ? "BDT"
                : profile.CurrencyCode.Trim();

        entity.CurrencySymbol =
            string.IsNullOrWhiteSpace(profile.CurrencySymbol)
                ? "৳"
                : profile.CurrencySymbol.Trim();

        entity.LogoPath =
            Clean(profile.LogoPath);
        entity.InvoiceAccentColor =
            string.IsNullOrWhiteSpace(
                profile.InvoiceAccentColor)
                ? "#4F56D9"
                : profile.InvoiceAccentColor.Trim();

        entity.InvoiceFooter =
            Clean(profile.InvoiceFooter);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return Map(entity);
    }

    private static BusinessProfileDto Map(
        BusinessProfile entity)
    {
        return new BusinessProfileDto
        {
            Id = entity.Id,

            BusinessName = entity.BusinessName,
            BusinessNameBn = entity.BusinessNameBn,

            Address = entity.Address,
            AddressBn = entity.AddressBn,

            Phone = entity.Phone,
            Email = entity.Email,
            Website = entity.Website,

            BinNumber = entity.BinNumber,
            TinNumber = entity.TinNumber,

            TradeLicenseNumber =
                entity.TradeLicenseNumber,

            CurrencyCode = entity.CurrencyCode,
            CurrencySymbol = entity.CurrencySymbol,

            LogoPath = entity.LogoPath,
            InvoiceAccentColor = entity.InvoiceAccentColor,

            InvoiceFooter = entity.InvoiceFooter
        };
    }

    private static string? Clean(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}