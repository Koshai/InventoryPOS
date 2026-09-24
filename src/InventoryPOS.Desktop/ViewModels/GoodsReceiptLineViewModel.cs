using InventoryPOS.Application.DTOs.Purchasing;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InventoryPOS.Desktop.ViewModels;

public class GoodsReceiptLineViewModel :
    INotifyPropertyChanged
{
    private double _receivingQuantity;
    private string _supplierBatchNumber =
        string.Empty;

    private DateTimeOffset?
    _manufactureDate =
        DateTimeOffset.Now.Date;

    private DateTimeOffset?
        _expiryDate =
            DateTimeOffset.Now.Date;

    private double _warrantyMonths;

    public Guid PurchaseOrderLineId { get; }

    public Guid ProductVariantId { get; }

    public string ProductName { get; }

    public string SKU { get; }

    public string UnitSymbol { get; }

    public decimal OrderedQuantity { get; }

    public decimal PreviouslyReceived { get; }

    public decimal RemainingQuantity { get; }

    public decimal UnitCost { get; }

    public double ReceivingQuantity
    {
        get => _receivingQuantity;
        set
        {
            if (_receivingQuantity == value)
                return;

            _receivingQuantity = value;

            OnPropertyChanged();
            OnPropertyChanged(
                nameof(ReceivingValueDisplay));
        }
    }

    public string SupplierBatchNumber
    {
        get => _supplierBatchNumber;
        set
        {
            if (_supplierBatchNumber == value)
                return;

            _supplierBatchNumber = value;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset? ManufactureDate
    {
        get => _manufactureDate;

        set
        {
            if (_manufactureDate == value)
                return;

            _manufactureDate = value;

            OnPropertyChanged();

            if (_manufactureDate.HasValue &&
                _expiryDate.HasValue &&
                _expiryDate.Value.Date <
                _manufactureDate.Value.Date)
            {
                ExpiryDate =
                    _manufactureDate.Value.Date;
            }
        }
    }

    public DateTimeOffset? ExpiryDate
    {
        get => _expiryDate;

        set
        {
            if (value.HasValue &&
                ManufactureDate.HasValue &&
                value.Value.Date <
                ManufactureDate.Value.Date)
            {
                value =
                    ManufactureDate.Value.Date;
            }

            if (_expiryDate == value)
                return;

            _expiryDate = value;

            OnPropertyChanged();
        }
    }

    public double WarrantyMonths
    {
        get => _warrantyMonths;
        set
        {
            if (_warrantyMonths == value)
                return;

            _warrantyMonths = value;
            OnPropertyChanged();
        }
    }

    public string ReceivingValueDisplay =>
        $"৳{Convert.ToDecimal(ReceivingQuantity) * UnitCost:N2}";

    public GoodsReceiptLineViewModel(
        ReceivePurchaseOrderLineDto line)
    {
        PurchaseOrderLineId =
            line.PurchaseOrderLineId;

        ProductVariantId =
            line.ProductVariantId;

        ProductName =
            line.ProductName;

        SKU =
            line.SKU;

        UnitSymbol =
            line.UnitSymbol;

        OrderedQuantity =
            line.OrderedQuantity;

        PreviouslyReceived =
            line.ReceivedQuantity;

        RemainingQuantity =
            line.RemainingQuantity;

        UnitCost =
            line.UnitCost;

        WarrantyMonths =
            line.WarrantyMonths ?? 0;
    }

    public event PropertyChangedEventHandler?
        PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
}