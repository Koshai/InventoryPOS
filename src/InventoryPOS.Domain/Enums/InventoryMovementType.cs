namespace InventoryPOS.Domain.Enums;

public enum InventoryMovementType
{
    OpeningStock = 1,

    PurchaseReceipt = 2,

    Sale = 3,

    CustomerReturn = 4,

    SupplierReturn = 5,

    AdjustmentIncrease = 6,

    AdjustmentDecrease = 7,

    TransferIn = 8,

    TransferOut = 9
}