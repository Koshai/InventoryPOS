using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InventoryPOS.Desktop.ViewModels;

public class PurchaseReturnLineViewModel
    : INotifyPropertyChanged
{
    private double _returnQuantity;

    public Guid GoodsReceiptLineId { get; set; }

    public Guid ProductVariantId { get; set; }

    public Guid InventoryBatchId { get; set; }

    public string ProductName { get; set; }
        = string.Empty;

    public string SKU { get; set; }
        = string.Empty;

    public string SupplierBatchNumber { get; set; }
        = string.Empty;

    public decimal ReceivedQuantity { get; set; }

    public decimal PreviouslyReturnedQuantity { get; set; }

    public decimal BatchQuantityOnHand { get; set; }

    public decimal MaximumReturnQuantity { get; set; }

    public decimal UnitCost { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public int? WarrantyMonths { get; set; }

    public string? Reason { get; set; }

    // NumberBox.Value is double in WinUI 3.
    public double ReturnQuantity
    {
        get => _returnQuantity;

        set
        {
            if (Math.Abs(
                    _returnQuantity - value) <
                0.000001)
            {
                return;
            }

            _returnQuantity = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(ReturnValue));
            OnPropertyChanged(
                nameof(ReturnValueDisplay));
        }
    }

    public double MaximumReturnQuantityDouble =>
        Convert.ToDouble(
            MaximumReturnQuantity);

    public decimal ReturnValue
    {
        get
        {
            if (double.IsNaN(ReturnQuantity) ||
                double.IsInfinity(ReturnQuantity) ||
                ReturnQuantity <= 0)
            {
                return 0;
            }

            return Convert.ToDecimal(
                       ReturnQuantity) *
                   UnitCost;
        }
    }

    public string ReceivedDisplay =>
        ReceivedQuantity.ToString("0.###");

    public string PreviouslyReturnedDisplay =>
        PreviouslyReturnedQuantity.ToString("0.###");

    public string BatchOnHandDisplay =>
        BatchQuantityOnHand.ToString("0.###");

    public string MaximumReturnDisplay =>
        MaximumReturnQuantity.ToString("0.###");

    public string UnitCostDisplay =>
        $"৳{UnitCost:N2}";

    public string ReturnValueDisplay =>
        $"৳{ReturnValue:N2}";

    public string ExpiryDisplay =>
        ExpiryDate.HasValue
            ? ExpiryDate.Value.ToString(
                "dd MMM yyyy")
            : "-";

    public string WarrantyDisplay =>
        WarrantyMonths.HasValue &&
        WarrantyMonths.Value > 0
            ? $"{WarrantyMonths.Value} months"
            : "-";

    public event PropertyChangedEventHandler?
        PropertyChanged;

    protected virtual void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
}