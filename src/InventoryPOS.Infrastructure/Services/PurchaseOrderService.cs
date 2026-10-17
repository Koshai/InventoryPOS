using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Application.Licensing;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly InventoryPosDbContext _dbContext;
    private readonly ILicenseAccessGuard _licenseAccessGuard;

    public PurchaseOrderService(
        InventoryPosDbContext dbContext,
        ILicenseAccessGuard licenseAccessGuard)
    {
        _dbContext = dbContext;
        _licenseAccessGuard = licenseAccessGuard;
    }

    public async Task<IReadOnlyList<PurchaseOrderListItemDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseOrders
            .AsNoTracking()
            .OrderByDescending(x => x.OrderDate)
            .Select(x => new PurchaseOrderListItemDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                OrderDate = x.OrderDate,
                ExpectedDate = x.ExpectedDate,
                SupplierName = x.Supplier!.Name,
                LocationName = x.Location!.Name,
                Status = x.Status.ToString(),
                LineCount = x.Lines.Count,
                TotalAmount = x.Lines.Sum(line =>
                    line.OrderedQuantity * line.UnitCost)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PurchaseOrderDetailsDto?> GetByIdAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.Id == purchaseOrderId)
            .Select(x => new PurchaseOrderDetailsDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                OrderDate = x.OrderDate,
                ExpectedDate = x.ExpectedDate,
                SupplierName = x.Supplier!.Name,
                SupplierCode = x.Supplier.SupplierCode,
                LocationName = x.Location!.Name,
                Status = x.Status.ToString(),
                Notes = x.Notes,

                Lines = x.Lines
                    .OrderBy(line => line.ProductVariant!.Product!.Name)
                    .Select(line => new PurchaseOrderLineDto
                    {
                        Id = line.Id,
                        ProductVariantId = line.ProductVariantId,
                        ProductName =
                            line.ProductVariant!.Product!.Name,
                        SKU = line.ProductVariant.SKU,
                        UnitSymbol =
                            line.ProductVariant.Product.Unit!.Symbol,
                        OrderedQuantity = line.OrderedQuantity,
                        ReceivedQuantity = line.ReceivedQuantity,
                        UnitCost = line.UnitCost,
                        WarrantyMonths = line.WarrantyMonths
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SupplierLookupDto>> GetSuppliersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Suppliers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SupplierLookupDto
            {
                Id = x.Id,
                SupplierCode = x.SupplierCode,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryProductLookupDto>>
        GetProductVariantsAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.Product!.IsActive)
            .OrderBy(x => x.Product!.Name)
            .ThenBy(x => x.SKU)
            .Select(x =>
                new InventoryProductLookupDto
                {
                    ProductVariantId = x.Id,

                    ProductName =
                        x.Product!.Name,

                    SKU =
                        x.SKU,

                    UnitSymbol =
                        x.Product.Unit!.Symbol
                })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LocationLookupDto>> GetLocationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new LocationLookupDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(
        CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        _licenseAccessGuard.EnsureCanWrite();
        if (request.SupplierId == Guid.Empty)
            throw new InvalidOperationException("Supplier is required.");

        if (request.LocationId == Guid.Empty)
            throw new InvalidOperationException("Location is required.");

        if (request.Lines.Count == 0)
            throw new InvalidOperationException(
                "At least one product is required.");

        var supplierExists =
            await _dbContext.Suppliers.AnyAsync(
                x => x.Id == request.SupplierId && x.IsActive,
                cancellationToken);

        if (!supplierExists)
            throw new InvalidOperationException(
                "The selected supplier is not available.");

        var locationExists =
            await _dbContext.Locations.AnyAsync(
                x => x.Id == request.LocationId && x.IsActive,
                cancellationToken);

        if (!locationExists)
            throw new InvalidOperationException(
                "The selected location is not available.");

        if (request.Lines
            .GroupBy(x => x.ProductVariantId)
            .Any(x => x.Count() > 1))
        {
            throw new InvalidOperationException(
                "The same product cannot appear more than once.");
        }

        foreach (var line in request.Lines)
        {
            if (line.ProductVariantId == Guid.Empty)
                throw new InvalidOperationException(
                    "A product is required for every line.");

            if (line.Quantity <= 0)
                throw new InvalidOperationException(
                    "Ordered quantity must be greater than zero.");

            if (line.UnitCost < 0)
                throw new InvalidOperationException(
                    "Unit cost cannot be negative.");

            if (line.WarrantyMonths < 0)
                throw new InvalidOperationException(
                    "Warranty months cannot be negative.");
        }

        var productIds =
            request.Lines
                .Select(x => x.ProductVariantId)
                .Distinct()
                .ToList();

        var validProductCount =
            await _dbContext.ProductVariants.CountAsync(
                x =>
                    productIds.Contains(x.Id) &&
                    x.IsActive &&
                    x.Product != null &&
                    x.Product.IsActive,
                cancellationToken);

        if (validProductCount != productIds.Count)
        {
            throw new InvalidOperationException(
                "One or more selected products are not available.");
        }

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = await GenerateOrderNumberAsync(
                cancellationToken),

            SupplierId = request.SupplierId,
            LocationId = request.LocationId,

            Status = PurchaseOrderStatus.Ordered,

            OrderDate = DateTime.UtcNow,

            ExpectedDate = request.ExpectedDate,

            Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? null
                : request.Notes.Trim()
        };

        foreach (var requestLine in request.Lines)
        {
            purchaseOrder.Lines.Add(
                new PurchaseOrderLine
                {
                    ProductVariantId =
                        requestLine.ProductVariantId,

                    OrderedQuantity =
                        requestLine.Quantity,

                    ReceivedQuantity = 0,

                    UnitCost =
                        requestLine.UnitCost,

                    WarrantyMonths =
                        requestLine.WarrantyMonths
                });
        }

        _dbContext.PurchaseOrders.Add(
            purchaseOrder);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return purchaseOrder.Id;
    }

    private async Task<string> GenerateOrderNumberAsync(
        CancellationToken cancellationToken)
    {
        var prefix =
            $"PO-{DateTime.UtcNow:yyyyMMdd}";

        var count =
            await _dbContext.PurchaseOrders
                .CountAsync(
                    x => x.OrderNumber.StartsWith(prefix),
                    cancellationToken);

        return $"{prefix}-{count + 1:000}";
    }
}