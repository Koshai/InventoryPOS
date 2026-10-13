using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Pricing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InventoryPOS.Desktop.ViewModels;

public class PosCartLineViewModel : INotifyPropertyChanged
{
    private double _quantity = 1;
    private readonly decimal? _quotedUnitPrice;

    private PricingAdjustmentType _discountType =
        PricingAdjustmentType.FixedAmount;

    private double _discountValue;

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
        _quotedUnitPrice ?? Product.SellingPrice;

    public bool HasQuotedPrice =>
        _quotedUnitPrice.HasValue;

    public decimal QuantityAvailable =>
        Product.QuantityAvailable;

    public double QuantityAvailableForNumberBox =>
        Convert.ToDouble(QuantityAvailable);

    // ---------------------------------------------------------
    // Quantity
    // ---------------------------------------------------------

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

    // ---------------------------------------------------------
    // Line Discount
    // ---------------------------------------------------------

    public IReadOnlyList<PricingAdjustmentType>
        DiscountTypes
    { get; } =
    [
        PricingAdjustmentType.FixedAmount,
        PricingAdjustmentType.Percentage
    ];

    public PricingAdjustmentType DiscountType
    {
        get => _discountType;

        set
        {
            if (_discountType == value)
                return;

            _discountType = value;

            OnPropertyChanged();
        }
    }

    public double DiscountValue
    {
        get => _discountValue;

        set
        {
            if (_discountValue == value)
                return;

            _discountValue = value;

            OnPropertyChanged();
        }
    }

    public decimal DecimalDiscountValue =>
        Convert.ToDecimal(DiscountValue);

    // ---------------------------------------------------------
    // Display Values
    // ---------------------------------------------------------

    public decimal LineTotal =>
        Convert.ToDecimal(Quantity) *
        UnitPrice;

    public string FormattedUnitPrice =>
        $"৳{UnitPrice:N2}";

    public string FormattedLineTotal =>
        $"৳{LineTotal:N2}";

    public string StockDisplay =>
        $"{QuantityAvailable:N3} {UnitSymbol}";

    // ---------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------

    public PosCartLineViewModel(
        PosProductDto product,
        decimal? quotedUnitPrice = null)
    {
        Product = product;
        _quotedUnitPrice = quotedUnitPrice;
    }

    // ---------------------------------------------------------
    // INotifyPropertyChanged
    // ---------------------------------------------------------

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