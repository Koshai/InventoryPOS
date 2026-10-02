using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Entities;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryPOS.Infrastructure.Services;

public class PurchaseRequisitionService : IPurchaseRequisitionService
{
    private readonly InventoryPosDbContext _dbContext;

    public PurchaseRequisitionService(
        InventoryPosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PurchaseRequisitionResultDto> CreateAsync(
        CreatePurchaseRequisitionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SupplierId == Guid.Empty)
            throw new InvalidOperationException(
                "A supplier is required.");

        if (request.LocationId == Guid.Empty)
            throw new InvalidOperationException(
                "A location is required.");

        if (request.Lines is null ||
            request.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one requisition line is required.");
        }

        if (request.Lines.Any(x =>
                x.ProductVariantId == Guid.Empty))
        {
            throw new InvalidOperationException(
                "Every requisition line must contain a product variant.");
        }

        if (request.Lines.Any(x =>
                x.RequestedQuantity <= 0))
        {
            throw new InvalidOperationException(
                "Requested quantity must be greater than zero.");
        }

        if (request.Lines.Any(x =>
                x.EstimatedUnitCost.HasValue &&
                x.EstimatedUnitCost.Value < 0))
        {
            throw new InvalidOperationException(
                "Estimated unit cost cannot be negative.");
        }

        var duplicateVariant =
            request.Lines
                .GroupBy(x => x.ProductVariantId)
                .FirstOrDefault(x => x.Count() > 1);

        if (duplicateVariant is not null)
        {
            throw new InvalidOperationException(
                "The same product variant cannot appear more than once.");
        }

        var supplierExists =
            await _dbContext.Suppliers
                .AnyAsync(
                    x => x.Id == request.SupplierId,
                    cancellationToken);

        if (!supplierExists)
        {
            throw new InvalidOperationException(
                "The selected supplier could not be found.");
        }

        var locationExists =
            await _dbContext.Locations
                .AnyAsync(
                    x => x.Id == request.LocationId,
                    cancellationToken);

        if (!locationExists)
        {
            throw new InvalidOperationException(
                "The selected location could not be found.");
        }

        var requestedVariantIds =
            request.Lines
                .Select(x => x.ProductVariantId)
                .Distinct()
                .ToList();

        var existingVariantIds =
            await _dbContext.ProductVariants
                .Where(x =>
                    requestedVariantIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

        if (existingVariantIds.Count !=
            requestedVariantIds.Count)
        {
            throw new InvalidOperationException(
                "One or more selected product variants could not be found.");
        }

        var requisitionNumber =
            await GenerateRequisitionNumberAsync(
                request.RequisitionDate,
                cancellationToken);

        var requisition =
            new PurchaseRequisition
            {
                RequisitionNumber = requisitionNumber,

                SupplierId = request.SupplierId,
                LocationId = request.LocationId,

                RequisitionDate =
                    request.RequisitionDate,

                RequiredByDate =
                    request.RequiredByDate,

                ValidUntilDate =
                    request.ValidUntilDate,

                Status =
                    PurchaseRequisitionStatus.Draft,

                Subject =
                    Normalize(request.Subject),

                Notes =
                    Normalize(request.Notes),

                Terms =
                    Normalize(request.Terms)
            };

        foreach (var requestedLine in request.Lines)
        {
            var estimatedLineTotal =
                requestedLine.EstimatedUnitCost.HasValue
                    ? requestedLine.RequestedQuantity *
                      requestedLine.EstimatedUnitCost.Value
                    : 0m;

            requisition.Lines.Add(
                new PurchaseRequisitionLine
                {
                    ProductVariantId =
                        requestedLine.ProductVariantId,

                    RequestedQuantity =
                        requestedLine.RequestedQuantity,

                    EstimatedUnitCost =
                        requestedLine.EstimatedUnitCost,

                    EstimatedLineTotal =
                        estimatedLineTotal,

                    Description =
                        Normalize(
                            requestedLine.Description),

                    Notes =
                        Normalize(
                            requestedLine.Notes)
                });
        }

        requisition.EstimatedTotal =
            requisition.Lines.Sum(
                x => x.EstimatedLineTotal);

        _dbContext.PurchaseRequisitions.Add(
            requisition);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new PurchaseRequisitionResultDto
        {
            PurchaseRequisitionId =
                requisition.Id,

            RequisitionNumber =
                requisition.RequisitionNumber,

            LineCount =
                requisition.Lines.Count,

            TotalRequestedQuantity =
                requisition.Lines.Sum(
                    x => x.RequestedQuantity),

            EstimatedTotal =
                requisition.EstimatedTotal
        };
    }

    public async Task<
        IReadOnlyList<PurchaseRequisitionHistoryDto>>
        GetHistoryAsync(
            string? search = null,
            CancellationToken cancellationToken = default)
    {
        var query =
            _dbContext.PurchaseRequisitions
                .AsNoTracking()
                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(x =>
                x.RequisitionNumber.Contains(term) ||
                (x.Subject != null &&
                 x.Subject.Contains(term)) ||
                (x.Supplier != null &&
                 x.Supplier.Name.Contains(term)));
        }

        return await query
            .OrderByDescending(x =>
                x.RequisitionDate)
            .ThenByDescending(x =>
                x.CreatedAtUtc)
            .Select(x =>
                new PurchaseRequisitionHistoryDto
                {
                    Id = x.Id,

                    RequisitionNumber =
                        x.RequisitionNumber,

                    RequisitionDate =
                        x.RequisitionDate,

                    SupplierName =
                        x.Supplier != null
                            ? x.Supplier.Name
                            : string.Empty,

                    LocationName =
                        x.Location != null
                            ? x.Location.Name
                            : string.Empty,

                    Status =
                        x.Status,

                    LineCount =
                        x.Lines.Count,

                    TotalRequestedQuantity =
                        x.Lines.Sum(
                            line =>
                                line.RequestedQuantity),

                    EstimatedTotal =
                        x.EstimatedTotal,

                    RequiredByDate =
                        x.RequiredByDate,

                    ValidUntilDate =
                        x.ValidUntilDate,

                    PurchaseOrderId =
                        x.PurchaseOrderId,

                    Subject =
                        x.Subject
                })
            .ToListAsync(cancellationToken);
    }

    public async Task<PurchaseRequisitionDetailsDto?>
        GetByIdAsync(
            Guid purchaseRequisitionId,
            CancellationToken cancellationToken = default)
    {
        if (purchaseRequisitionId == Guid.Empty)
            return null;

        return await _dbContext
            .PurchaseRequisitions
            .AsNoTracking()
            .Where(x =>
                x.Id == purchaseRequisitionId)
            .Select(x =>
                new PurchaseRequisitionDetailsDto
                {
                    Id = x.Id,

                    RequisitionNumber =
                        x.RequisitionNumber,

                    RequisitionDate =
                        x.RequisitionDate,

                    RequiredByDate =
                        x.RequiredByDate,

                    ValidUntilDate =
                        x.ValidUntilDate,

                    Status =
                        x.Status,

                    SupplierId =
                        x.SupplierId,

                    SupplierName =
                        x.Supplier != null
                            ? x.Supplier.Name
                            : string.Empty,

                    LocationId =
                        x.LocationId,

                    LocationName =
                        x.Location != null
                            ? x.Location.Name
                            : string.Empty,

                    Subject =
                        x.Subject,

                    Notes =
                        x.Notes,

                    Terms =
                        x.Terms,

                    EstimatedTotal =
                        x.EstimatedTotal,

                    PurchaseOrderId =
                        x.PurchaseOrderId,

                    PurchaseOrderNumber =
                        x.PurchaseOrder != null
                            ? x.PurchaseOrder.OrderNumber
                            : null,

                    Lines =
                        x.Lines
                            .OrderBy(line =>
                                line.CreatedAtUtc)
                            .Select(line =>
                                new PurchaseRequisitionDetailsLineDto
                                {
                                    Id =
                                        line.Id,

                                    ProductVariantId =
                                        line.ProductVariantId,

                                    ProductName =
                                        line.ProductVariant != null &&
                                        line.ProductVariant.Product != null
                                            ? line.ProductVariant.Product.Name
                                            : string.Empty,

                                    SKU =
                                        line.ProductVariant != null
                                            ? line.ProductVariant.SKU
                                            : string.Empty,

                                    RequestedQuantity =
                                        line.RequestedQuantity,

                                    EstimatedUnitCost =
                                        line.EstimatedUnitCost,

                                    EstimatedLineTotal =
                                        line.EstimatedLineTotal,

                                    Description =
                                        line.Description,

                                    Notes =
                                        line.Notes
                                })
                            .ToList()
                })
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    public async Task MarkAsSentAsync(
    Guid purchaseRequisitionId,
    CancellationToken cancellationToken = default)
    {
        if (purchaseRequisitionId == Guid.Empty)
            throw new ArgumentException(
                "Purchase requisition ID is required.",
                nameof(purchaseRequisitionId));

        var requisition =
            await _dbContext.PurchaseRequisitions
                .FirstOrDefaultAsync(
                    x => x.Id == purchaseRequisitionId,
                    cancellationToken);

        if (requisition is null)
            throw new InvalidOperationException(
                "Purchase requisition was not found.");

        if (requisition.Status !=
            PurchaseRequisitionStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Only Draft requisitions can be marked as Sent. " +
                $"Current status: {requisition.Status}.");
        }

        requisition.Status =
            PurchaseRequisitionStatus.Sent;

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<string>
        GenerateRequisitionNumberAsync(
            DateTime requisitionDate,
            CancellationToken cancellationToken)
    {
        var date =
            requisitionDate == default
                ? DateTime.UtcNow
                : requisitionDate;

        var prefix =
            $"PRQ-{date:yyyyMMdd}-";

        var count =
            await _dbContext
                .PurchaseRequisitions
                .CountAsync(
                    x => x.RequisitionNumber
                        .StartsWith(prefix),
                    cancellationToken);

        return $"{prefix}{count + 1:000}";
    }

    public async Task CancelAsync(
    Guid purchaseRequisitionId,
    CancellationToken cancellationToken = default)
    {
        if (purchaseRequisitionId == Guid.Empty)
            throw new ArgumentException(
                "Purchase requisition ID is required.",
                nameof(purchaseRequisitionId));

        var requisition =
            await _dbContext.PurchaseRequisitions
                .FirstOrDefaultAsync(
                    x => x.Id == purchaseRequisitionId,
                    cancellationToken);

        if (requisition is null)
            throw new InvalidOperationException(
                "Purchase requisition was not found.");

        if (requisition.Status ==
            PurchaseRequisitionStatus.ConvertedToPurchaseOrder)
        {
            throw new InvalidOperationException(
                "A requisition that has already been converted " +
                "to a Purchase Order cannot be cancelled.");
        }

        if (requisition.Status ==
            PurchaseRequisitionStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "This requisition is already cancelled.");
        }

        if (requisition.Status !=
                PurchaseRequisitionStatus.Draft &&
            requisition.Status !=
                PurchaseRequisitionStatus.Sent)
        {
            throw new InvalidOperationException(
                $"A requisition with status {requisition.Status} " +
                "cannot be cancelled.");
        }

        requisition.Status =
            PurchaseRequisitionStatus.Cancelled;

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<PurchaseRequisitionToPurchaseOrderDto>
    GetPurchaseOrderConversionAsync(
        Guid purchaseRequisitionId,
        CancellationToken cancellationToken = default)
    {
        if (purchaseRequisitionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Purchase requisition ID is required.",
                nameof(purchaseRequisitionId));
        }

        var requisition =
            await _dbContext.PurchaseRequisitions
                .AsNoTracking()
                .Where(x =>
                    x.Id == purchaseRequisitionId)
                .Select(x =>
                    new PurchaseRequisitionToPurchaseOrderDto
                    {
                        PurchaseRequisitionId = x.Id,

                        SupplierId = x.SupplierId,

                        LocationId = x.LocationId,

                        ExpectedDate =
                            x.RequiredByDate,

                        Notes = x.Notes,

                        Lines = x.Lines
                            .Select(line =>
                                new PurchaseRequisitionToPurchaseOrderLineDto
                                {
                                    ProductVariantId =
                                        line.ProductVariantId,

                                    Quantity =
                                        line.RequestedQuantity,

                                    EstimatedUnitCost =
                                        line.EstimatedUnitCost
                                })
                            .ToList()
                    })
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (requisition is null)
        {
            throw new InvalidOperationException(
                "Purchase requisition was not found.");
        }

        var status =
            await _dbContext.PurchaseRequisitions
                .AsNoTracking()
                .Where(x =>
                    x.Id == purchaseRequisitionId)
                .Select(x => x.Status)
                .FirstAsync(cancellationToken);

        if (status != PurchaseRequisitionStatus.Sent &&
            status != PurchaseRequisitionStatus.Approved)
        {
            throw new InvalidOperationException(
                "Only a Sent or Approved requisition " +
                "can be converted to a Purchase Order.");
        }

        return requisition;
    }

    public async Task<Guid> ConvertToPurchaseOrderAsync(
    Guid purchaseRequisitionId,
    CreatePurchaseOrderRequest request,
    CancellationToken cancellationToken = default)
    {
        if (purchaseRequisitionId == Guid.Empty)
            throw new InvalidOperationException(
                "Purchase requisition is required.");

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var requisition =
                await _dbContext.PurchaseRequisitions
                    .FirstOrDefaultAsync(
                        x => x.Id == purchaseRequisitionId,
                        cancellationToken);

            if (requisition is null)
                throw new InvalidOperationException(
                    "Purchase requisition was not found.");

            if (requisition.Status ==
                    PurchaseRequisitionStatus
                        .ConvertedToPurchaseOrder ||
                requisition.PurchaseOrderId.HasValue)
            {
                throw new InvalidOperationException(
                    "This requisition has already been " +
                    "converted to a Purchase Order.");
            }

            if (requisition.Status !=
                    PurchaseRequisitionStatus.Sent &&
                requisition.Status !=
                    PurchaseRequisitionStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Only a Sent or Approved requisition " +
                    "can be converted to a Purchase Order.");
            }

            if (request.SupplierId != requisition.SupplierId)
            {
                throw new InvalidOperationException(
                    "The Purchase Order supplier must match " +
                    "the requisition supplier.");
            }

            if (request.LocationId != requisition.LocationId)
            {
                throw new InvalidOperationException(
                    "The Purchase Order location must match " +
                    "the requisition location.");
            }

            var requisitionProductIds =
                await _dbContext.PurchaseRequisitionLines
                    .Where(x =>
                        x.PurchaseRequisitionId ==
                        purchaseRequisitionId)
                    .Select(x => x.ProductVariantId)
                    .ToListAsync(cancellationToken);

            var purchaseOrderProductIds =
                request.Lines
                    .Select(x => x.ProductVariantId)
                    .ToList();

            if (requisitionProductIds.Count !=
                    purchaseOrderProductIds.Count ||
                requisitionProductIds
                    .Except(purchaseOrderProductIds)
                    .Any())
            {
                throw new InvalidOperationException(
                    "The Purchase Order products must match " +
                    "the products on the requisition.");
            }

            ValidatePurchaseOrderRequest(request);

            var productIds =
                request.Lines
                    .Select(x => x.ProductVariantId)
                    .Distinct()
                    .ToList();

            var validProductCount =
                await _dbContext.ProductVariants
                    .CountAsync(
                        x =>
                            productIds.Contains(x.Id) &&
                            x.IsActive &&
                            x.Product != null &&
                            x.Product.IsActive,
                        cancellationToken);

            if (validProductCount != productIds.Count)
            {
                throw new InvalidOperationException(
                    "One or more selected products " +
                    "are not available.");
            }

            var purchaseOrder =
                new PurchaseOrder
                {
                    OrderNumber =
                        await GeneratePurchaseOrderNumberAsync(
                            cancellationToken),

                    SupplierId =
                        request.SupplierId,

                    LocationId =
                        request.LocationId,

                    Status =
                        PurchaseOrderStatus.Ordered,

                    OrderDate =
                        DateTime.UtcNow,

                    ExpectedDate =
                        request.ExpectedDate,

                    Notes =
                        string.IsNullOrWhiteSpace(request.Notes)
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

            requisition.PurchaseOrderId =
                purchaseOrder.Id;

            requisition.Status =
                PurchaseRequisitionStatus
                    .ConvertedToPurchaseOrder;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return purchaseOrder.Id;
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static void ValidatePurchaseOrderRequest(
        CreatePurchaseOrderRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (request.SupplierId == Guid.Empty)
            throw new InvalidOperationException(
                "Supplier is required.");

        if (request.LocationId == Guid.Empty)
            throw new InvalidOperationException(
                "Location is required.");

        if (request.Lines is null ||
            request.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one product is required.");
        }

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
    }

    private async Task<string>
    GeneratePurchaseOrderNumberAsync(
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

    private static string? Normalize(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}