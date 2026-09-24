using InventoryPOS.Application.DTOs.Sales;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InventoryPOS.Desktop.ViewModels;

public class PosCartLineViewModel : INotifyPropertyChanged
{
    private double _quantity = 1;

    public PosProductDto Product { get; }

    public Guid ProductVariantId =>
        Product.ProductVariantId;

    public string SKU =>
        Product.SKU;

    public string ProductName =>
        Product.ProductName;

    public string UnitSymbol =>
        Product.UnitSymbol;

    public decimal UnitPrice =>
        Product.SellingPrice;

    public decimal QuantityAvailable =>
    Product.QuantityAvailable;

    public double QuantityAvailableForNumberBox =>
        Convert.ToDouble(Product.QuantityAvailable);

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
            OnPropertyChanged(nameof(FormattedLineTotal));
        }
    }

    public decimal LineTotal =>
        Convert.ToDecimal(Quantity) * UnitPrice;

    public string FormattedUnitPrice =>
        $"৳{UnitPrice:N2}";

    public string FormattedLineTotal =>
        $"৳{LineTotal:N2}";

    public string StockDisplay =>
        $"{QuantityAvailable:N3} {UnitSymbol}";

    public PosCartLineViewModel(
        PosProductDto product)
    {
        Product = product;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}