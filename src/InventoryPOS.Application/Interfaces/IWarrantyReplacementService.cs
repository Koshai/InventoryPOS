
using InventoryPOS.Application.DTOs.WarrantyClaims;

namespace InventoryPOS.Application.Interfaces;

public interface IWarrantyReplacementService
{
    Task<IssueWarrantyReplacementResult> IssueAsync(
        IssueWarrantyReplacementRequest request,
        CancellationToken cancellationToken = default);
}
