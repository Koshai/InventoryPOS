
using InventoryPOS.Application.DTOs.WarrantyClaims;

namespace InventoryPOS.Application.Interfaces;

public interface IWarrantyClaimManagementService
{
    Task StartInspectionAsync(
        Guid claimId,
        CancellationToken cancellationToken = default);

    Task SaveInspectionAsync(
        SaveWarrantyInspectionRequest request,
        CancellationToken cancellationToken = default);

    Task ApproveAsync(
        DecideWarrantyClaimRequest request,
        CancellationToken cancellationToken = default);

    Task RejectAsync(
        DecideWarrantyClaimRequest request,
        CancellationToken cancellationToken = default);

    Task StartRepairAsync(
        StartWarrantyProcessingRequest request,
        CancellationToken cancellationToken = default);

    Task PrepareReplacementAsync(
        StartWarrantyProcessingRequest request,
        CancellationToken cancellationToken = default);

    Task SaveTechnicianNotesAsync(
        Guid claimId,
        Guid claimItemId,
        string technicianNotes,
        CancellationToken cancellationToken = default);

    Task CompleteRepairAsync(
    CompleteWarrantyRepairRequest request,
    CancellationToken cancellationToken = default);

}
