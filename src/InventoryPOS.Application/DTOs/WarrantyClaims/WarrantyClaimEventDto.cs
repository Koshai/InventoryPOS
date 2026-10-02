using System;

namespace InventoryPOS.Application.DTOs.WarrantyClaims;

public sealed class WarrantyClaimEventDto
{
    public Guid Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? FromStatus { get; set; }

    public string? ToStatus { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string CreatedAtDisplay =>
        CreatedAtUtc
            .ToLocalTime()
            .ToString("dd MMM yyyy HH:mm");

    public string StatusTransitionDisplay
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FromStatus) &&
                string.IsNullOrWhiteSpace(ToStatus))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(FromStatus))
            {
                return ToStatus ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(ToStatus))
            {
                return FromStatus;
            }

            if (string.Equals(
                    FromStatus,
                    ToStatus,
                    StringComparison.Ordinal))
            {
                return ToStatus;
            }

            return $"{FromStatus} → {ToStatus}";
        }
    }

    public bool HasNotes =>
        !string.IsNullOrWhiteSpace(Notes);

    public bool HasStatusTransition =>
        !string.IsNullOrWhiteSpace(
            StatusTransitionDisplay);
}