using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class StockAdjustmentViewModel : INotifyPropertyChanged
{
    private readonly IInventoryService _inventoryService;
    private readonly IStockAdjustmentService _stockAdjustmentService;

    private InventoryProductLookupDto? _selectedProduct;
    private LocationLookupDto? _selectedLocation;

    private decimal _systemQuantity;
    private bool _hasStockBalance;

    private double _countedQuantity;

    private string _reason = string.Empty;
    private string? _notes;

    private bool _isLoading;
    private bool _isSaving;
    private string? _errorMessage;
    private string? _successMessage;

    public ObservableCollection<InventoryProductLookupDto> Products { get; }
        = new();

    public ObservableCollection<LocationLookupDto> Locations { get; }
        = new();

    public InventoryProductLookupDto? SelectedProduct
    {
        get => _selectedProduct;

        set
        {
            if (_selectedProduct == value)
                return;

            _selectedProduct = value;

            OnPropertyChanged();

            _ = RefreshCurrentQuantityAsync();
        }
    }

    public LocationLookupDto? SelectedLocation
    {
        get => _selectedLocation;

        set
        {
            if (_selectedLocation == value)
                return;

            _selectedLocation = value;

            OnPropertyChanged();

            _ = RefreshCurrentQuantityAsync();
        }
    }

    public decimal SystemQuantity
    {
        get => _systemQuantity;

        private set
        {
            if (_systemQuantity == value)
                return;

            _systemQuantity = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(Difference));
            OnPropertyChanged(nameof(SystemQuantityDisplay));
            OnPropertyChanged(nameof(DifferenceDisplay));
        }
    }

    public string SystemQuantityDisplay =>
        HasStockBalance
            ? FormatQuantity(SystemQuantity)
            : "—";

    public bool HasStockBalance
    {
        get => _hasStockBalance;

        private set
        {
            if (_hasStockBalance == value)
                return;

            _hasStockBalance = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(SystemQuantityDisplay));
        }
    }

    public double CountedQuantity
    {
        get => _countedQuantity;

        set
        {
            if (Math.Abs(_countedQuantity - value) < 0.000001)
                return;

            _countedQuantity = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(Difference));
            OnPropertyChanged(nameof(DifferenceDisplay));
        }
    }

    public decimal Difference =>
        Convert.ToDecimal(CountedQuantity) -
        SystemQuantity;

    public string DifferenceDisplay
    {
        get
        {
            if (!HasStockBalance)
                return "—";

            var difference = Difference;

            if (difference > 0)
                return $"+{FormatQuantity(difference)}";

            return FormatQuantity(difference);
        }
    }

    public string Reason
    {
        get => _reason;

        set
        {
            if (_reason == value)
                return;

            _reason = value;

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

    public bool IsLoading
    {
        get => _isLoading;

        private set
        {
            if (_isLoading == value)
                return;

            _isLoading = value;

            OnPropertyChanged();
        }
    }

    public bool IsSaving
    {
        get => _isSaving;

        private set
        {
            if (_isSaving == value)
                return;

            _isSaving = value;

            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;

        private set
        {
            if (_errorMessage == value)
                return;

            _errorMessage = value;

            OnPropertyChanged();
        }
    }

    public string? SuccessMessage
    {
        get => _successMessage;

        private set
        {
            if (_successMessage == value)
                return;

            _successMessage = value;

            OnPropertyChanged();
        }
    }

    public StockAdjustmentViewModel(
        IInventoryService inventoryService,
        IStockAdjustmentService stockAdjustmentService)
    {
        _inventoryService = inventoryService;
        _stockAdjustmentService = stockAdjustmentService;
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;

            ErrorMessage = null;
            SuccessMessage = null;

            Products.Clear();
            Locations.Clear();

            //
            // Important:
            // Keep these sequential because both services may use
            // the same scoped DbContext.
            //

            var products =
                await _inventoryService
                    .GetProductVariantsAsync();

            var locations =
                await _inventoryService
                    .GetLocationsAsync();

            foreach (var product in products)
            {
                Products.Add(product);
            }

            foreach (var location in locations)
            {
                Locations.Add(location);
            }

            if (Locations.Count == 1)
            {
                SelectedLocation =
                    Locations[0];
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshCurrentQuantityAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (SelectedProduct is null ||
            SelectedLocation is null)
        {
            HasStockBalance = false;
            SystemQuantity = 0;

            return;
        }

        try
        {
            var quantity =
                await _stockAdjustmentService
                    .GetCurrentQuantityAsync(
                        SelectedProduct.ProductVariantId,
                        SelectedLocation.Id);

            if (!quantity.HasValue)
            {
                HasStockBalance = false;
                SystemQuantity = 0;

                ErrorMessage =
                    "No stock balance exists for this product at this location. Enter opening stock first.";

                return;
            }

            HasStockBalance = true;

            SystemQuantity =
                quantity.Value;

            //
            // Start Counted Quantity at the current system quantity.
            //
            // This makes the form safer. The user changes it only
            // when the physical count is different.
            //

            CountedQuantity =
                Convert.ToDouble(
                    quantity.Value);
        }
        catch (Exception ex)
        {
            HasStockBalance = false;
            SystemQuantity = 0;

            ErrorMessage =
                ex.Message;
        }
    }

    public async Task<bool> SaveAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (SelectedProduct is null)
        {
            ErrorMessage =
                "Product is required.";

            return false;
        }

        if (SelectedLocation is null)
        {
            ErrorMessage =
                "Location is required.";

            return false;
        }

        if (!HasStockBalance)
        {
            ErrorMessage =
                "No stock balance exists for this product at this location.";

            return false;
        }

        if (CountedQuantity < 0)
        {
            ErrorMessage =
                "Counted quantity cannot be negative.";

            return false;
        }

        if (string.IsNullOrWhiteSpace(Reason))
        {
            ErrorMessage =
                "Adjustment reason is required.";

            return false;
        }

        try
        {
            IsSaving = true;

            var result =
                await _stockAdjustmentService
                    .CreateAsync(
                        new CreateStockAdjustmentRequest
                        {
                            ProductVariantId =
                                SelectedProduct.ProductVariantId,

                            LocationId =
                                SelectedLocation.Id,

                            CountedQuantity =
                                Convert.ToDecimal(
                                    CountedQuantity),

                            Reason =
                                Reason,

                            Notes =
                                Notes
                        });

            SuccessMessage =
                $"Stock adjusted from " +
                $"{FormatQuantity(result.PreviousQuantity)} " +
                $"to {FormatQuantity(result.CountedQuantity)}. " +
                $"Difference: " +
                $"{FormatSignedQuantity(result.Difference)}.";

            SystemQuantity =
                result.CountedQuantity;

            CountedQuantity =
                Convert.ToDouble(
                    result.CountedQuantity);

            Reason = string.Empty;
            Notes = null;

            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;

            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private static string FormatQuantity(
        decimal quantity)
    {
        return quantity % 1 == 0
            ? quantity.ToString("N0")
            : quantity.ToString("N3")
                .TrimEnd('0')
                .TrimEnd('.');
    }

    private static string FormatSignedQuantity(
        decimal quantity)
    {
        if (quantity > 0)
        {
            return $"+{FormatQuantity(quantity)}";
        }

        return FormatQuantity(quantity);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
}