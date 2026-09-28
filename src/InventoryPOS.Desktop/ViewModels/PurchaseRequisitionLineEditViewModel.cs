using InventoryPOS.Application.DTOs.Inventory;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InventoryPOS.Desktop.ViewModels;

public class PurchaseRequisitionLineEditViewModel
    : INotifyPropertyChanged
{
    private InventoryProductLookupDto? _selectedProduct;
    private double _quantity = 1;
    private double _estimatedUnitCost;
    private bool _hasEstimatedCost;
    private string? _description;
    private string? _notes;

    public InventoryProductLookupDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (_selectedProduct == value)
                return;

            _selectedProduct = value;
            OnPropertyChanged();
        }
    }

    public double Quantity
    {
        get => _quantity;
        set
        {
            if (Math.Abs(_quantity - value) < 0.000001)
                return;

            _quantity = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(EstimatedLineTotal));
            OnPropertyChanged(nameof(EstimatedLineTotalDisplay));
        }
    }

    public double EstimatedUnitCost
    {
        get => _estimatedUnitCost;
        set
        {
            if (Math.Abs(_estimatedUnitCost - value) < 0.000001)
                return;

            _estimatedUnitCost = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(EstimatedLineTotal));
            OnPropertyChanged(nameof(EstimatedLineTotalDisplay));
        }
    }

    public bool HasEstimatedCost
    {
        get => _hasEstimatedCost;
        set
        {
            if (_hasEstimatedCost == value)
                return;

            _hasEstimatedCost = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(EstimatedLineTotal));
            OnPropertyChanged(nameof(EstimatedLineTotalDisplay));
        }
    }

    public string? Description
    {
        get => _description;
        set
        {
            if (_description == value)
                return;

            _description = value;
            OnPropertyChanged();
        }
    }

    public string? Notes
    {
        get => _notes;
        set
        {
            if (_notes == value)
                return;

            _notes = value;
            OnPropertyChanged();
        }
    }

    public decimal EstimatedLineTotal
    {
        get
        {
            if (!HasEstimatedCost)
                return 0m;

            return Convert.ToDecimal(Quantity) *
                   Convert.ToDecimal(EstimatedUnitCost);
        }
    }

    public string EstimatedLineTotalDisplay =>
        HasEstimatedCost
            ? $"৳{EstimatedLineTotal:N2}"
            : "Price requested";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}