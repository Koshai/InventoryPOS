using InventoryPOS.Application.DTOs.Inventory;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InventoryPOS.Desktop.ViewModels;

public class PurchaseOrderLineEditViewModel
    : INotifyPropertyChanged
{
    private InventoryProductLookupDto? _selectedProduct;
    private double _quantity = 1;
    private double _unitCost;
    private double _warrantyMonths;
    private bool _requiresUnitCost;

    public InventoryProductLookupDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (_selectedProduct == value)
                return;

            _selectedProduct = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LineTotalDisplay));
        }
    }

    public double Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity == value)
                return;

            _quantity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LineTotal));
            OnPropertyChanged(nameof(LineTotalDisplay));
        }
    }

    public double UnitCost
    {
        get => _unitCost;
        set
        {
            if (_unitCost == value)
                return;

            _unitCost = value;
            if (value > 0)
                RequiresUnitCost = false;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LineTotal));
            OnPropertyChanged(nameof(LineTotalDisplay));
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

    public decimal LineTotal =>
        Convert.ToDecimal(Quantity) *
        Convert.ToDecimal(UnitCost);

    public string LineTotalDisplay =>
        $"৳{LineTotal:N2}";

    public bool RequiresUnitCost
    {
        get => _requiresUnitCost;
        set
        {
            if (_requiresUnitCost == value)
                return;

            _requiresUnitCost = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler?
        PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}