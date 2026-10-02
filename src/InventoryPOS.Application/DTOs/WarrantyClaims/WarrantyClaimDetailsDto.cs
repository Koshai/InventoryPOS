namespace InventoryPOS.Application.DTOs.WarrantyClaims;

public sealed class WarrantyClaimDetailsDto
{
    public Guid Id { get; set; }

    public string ClaimNumber { get; set; } =
        string.Empty;

    public DateTime ClaimDate { get; set; }

    public string Status { get; set; } =
        string.Empty;

    public string InvoiceNumber { get; set; } =
        string.Empty;

    public string CustomerName { get; set; } =
        string.Empty;

    public string? CustomerPhone { get; set; }

    public string? CustomerComplaint { get; set; }

    public string? InspectionNotes { get; set; }

    public string? ResolutionNotes { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    public List<WarrantyClaimDetailItemDto> Items
    {
        get;
        set;
    } = new();

    public List<WarrantyReplacementHistoryDto> Replacements
    {
        get;
        set;
    } = new();

    // W6.1 - Persisted warranty audit timeline.
    public List<WarrantyClaimEventDto> Events
    {
        get;
        set;
    } = new();

    public string ClaimDateDisplay =>
        ClaimDate
            .ToLocalTime()
            .ToString("dd MMM yyyy HH:mm");
}