
namespace InventoryPOS.Application.DTOs.WarrantyClaims;

public sealed class WarrantyClaimPageDto
{
    public List<WarrantyClaimListItemDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
