namespace InventoryPOS.Domain.Enums;

public enum WarrantyClaimEventType
{
    ClaimSubmitted = 0,
    InspectionStarted = 1,
    InspectionUpdated = 2,
    ClaimApproved = 3,
    ClaimRejected = 4,
    RepairStarted = 5,
    RepairCompleted = 6,
    ReplacementPrepared = 7,
    ReplacementIssued = 8,
    ClaimResolved = 9,
    ClaimCancelled = 10,
    TechnicianNotesUpdated = 11
}