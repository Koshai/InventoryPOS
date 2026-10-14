using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InventoryPOS.Desktop.ViewModels;

public sealed class SalesReturnLineViewModel : INotifyPropertyChanged
{
    private double _returnQuantity;
    private bool _isRestockable = true;
    private string? _reason;

    public Guid SaleLineId { get; init; }
    public Guid ProductVariantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string SKU { get; init; } = string.Empty;
    public decimal SoldQuantity { get; init; }
    public decimal ReturnedQuantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal OriginalLineTotal { get; init; }
    public decimal OriginalRefundableTotal { get; init; }
    public decimal PreviouslyReturnedTotal { get; init; }

    public decimal ReturnableQuantity => Math.Max(0m, SoldQuantity - ReturnedQuantity);
    public double MaximumReturnQuantity => Convert.ToDouble(ReturnableQuantity);

    public double ReturnQuantity
    {
        get => _returnQuantity;
        set
        {
            if (Math.Abs(_returnQuantity - value) < 0.000001) return;
            _returnQuantity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EstimatedReturnAmount));
            OnPropertyChanged(nameof(FormattedEstimatedReturn));
            ReturnQuantityChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsRestockable
    {
        get => _isRestockable;
        set
        {
            if (_isRestockable == value) return;
            _isRestockable = value;
            OnPropertyChanged();
        }
    }

    public string? Reason
    {
        get => _reason;
        set
        {
            if (_reason == value) return;
            _reason = value;
            OnPropertyChanged();
        }
    }

    public decimal EstimatedReturnAmount
    {
        get
        {
            if (SoldQuantity <= 0 || ReturnQuantity <= 0) return 0m;
            var quantity = Convert.ToDecimal(ReturnQuantity);
            var remaining = Math.Round(
                OriginalRefundableTotal - PreviouslyReturnedTotal,
                2, MidpointRounding.AwayFromZero);
            if (remaining < 0m) return 0m;
            if (quantity == ReturnableQuantity) return remaining;
            var proportional = Math.Round(
                OriginalRefundableTotal * quantity / SoldQuantity,
                2, MidpointRounding.AwayFromZero);
            return Math.Min(remaining, proportional);
        }
    }

    public string FormattedUnitPrice => $"৳{UnitPrice:N2}";
    public string FormattedEstimatedReturn => $"৳{EstimatedReturnAmount:N2}";
    public event EventHandler? ReturnQuantityChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
