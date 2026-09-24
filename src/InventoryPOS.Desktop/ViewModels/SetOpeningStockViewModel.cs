using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class SetOpeningStockViewModel : INotifyPropertyChanged
{
    private readonly IInventoryService _inventoryService;

    private InventoryProductLookupDto? _selectedProduct;
    private LocationLookupDto? _selectedLocation;

    private double _quantity;
    private double _unitCost;

    private string? _notes;
    private string? _errorMessage;

    private bool _isSaving;

    public ObservableCollection<InventoryProductLookupDto> Products { get; }
        = new();

    public ObservableCollection<LocationLookupDto> Locations { get; }
        = new();

    public InventoryProductLookupDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            _selectedProduct = value;
            OnPropertyChanged();
        }
    }

    public LocationLookupDto? SelectedLocation
    {
        get => _selectedLocation;
        set
        {
            _selectedLocation = value;
            OnPropertyChanged();
        }
    }

    public double Quantity
    {
        get => _quantity;
        set
        {
            _quantity = value;
            OnPropertyChanged();
        }
    }

    public double UnitCost
    {
        get => _unitCost;
        set
        {
            _unitCost = value;
            OnPropertyChanged();
        }
    }

    public string? Notes
    {
        get => _notes;
        set
        {
            _notes = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            _isSaving = value;
            OnPropertyChanged();
        }
    }

    public SetOpeningStockViewModel(
        IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task LoadAsync()
    {
        var products =
            await _inventoryService
                .GetProductVariantsAsync();

        var locations =
            await _inventoryService
                .GetLocationsAsync();

        Products.Clear();

        foreach (var product in products)
        {
            Products.Add(product);
        }

        Locations.Clear();

        foreach (var location in locations)
        {
            Locations.Add(location);
        }

        SelectedProduct =
            Products.FirstOrDefault();

        SelectedLocation =
            Locations.FirstOrDefault();
    }

    public async Task<bool> SaveAsync()
    {
        ErrorMessage = null;

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

        if (Quantity <= 0)
        {
            ErrorMessage =
                "Quantity must be greater than zero.";

            return false;
        }

        try
        {
            IsSaving = true;

            await _inventoryService
                .SetOpeningStockAsync(
                    new SetOpeningStockRequest
                    {
                        ProductVariantId =
                            SelectedProduct.ProductVariantId,

                        LocationId =
                            SelectedLocation.Id,

                        Quantity =
                            Convert.ToDecimal(Quantity),

                        UnitCost =
                            UnitCost > 0
                                ? Convert.ToDecimal(UnitCost)
                                : null,

                        Notes = Notes
                    });

            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;

            return false;
        }
        finally
        {
            IsSaving = false;
        }
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