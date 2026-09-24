using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class CreatePurchaseOrderViewModel
    : INotifyPropertyChanged
{
    private readonly IPurchaseOrderService _purchaseOrderService;

    private SupplierLookupDto? _selectedSupplier;
    private LocationLookupDto? _selectedLocation;
    private DateTimeOffset? _expectedDate;
    private string? _notes;
    private bool _isLoading;
    private bool _isSaving;
    private string? _errorMessage;

    public ObservableCollection<SupplierLookupDto>
        Suppliers
    { get; } = new();

    public ObservableCollection<LocationLookupDto>
        Locations
    { get; } = new();

    public ObservableCollection<InventoryProductLookupDto>
        Products
    { get; } = new();

    public ObservableCollection<PurchaseOrderLineEditViewModel>
        Lines
    { get; } = new();

    public SupplierLookupDto? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (_selectedSupplier == value)
                return;

            _selectedSupplier = value;
            OnPropertyChanged();
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
        }
    }

    public DateTimeOffset? ExpectedDate
    {
        get => _expectedDate;
        set
        {
            if (_expectedDate == value)
                return;

            _expectedDate = value;
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

    public decimal TotalAmount =>
        Lines.Sum(x => x.LineTotal);

    public string TotalAmountDisplay =>
        $"৳{TotalAmount:N2}";

    public CreatePurchaseOrderViewModel(
        IPurchaseOrderService purchaseOrderService)
    {
        _purchaseOrderService = purchaseOrderService;

        Lines.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(TotalAmountDisplay));
        };
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            Suppliers.Clear();
            Locations.Clear();
            Products.Clear();

            // Sequential intentionally:
            // all use the same scoped DbContext.
            var suppliers =
                await _purchaseOrderService.GetSuppliersAsync();

            foreach (var supplier in suppliers)
                Suppliers.Add(supplier);

            var locations =
                await _purchaseOrderService.GetLocationsAsync();

            foreach (var location in locations)
                Locations.Add(location);

            var products =
                await _purchaseOrderService.GetProductVariantsAsync();

            foreach (var product in products)
                Products.Add(product);

            if (Locations.Count == 1)
                SelectedLocation = Locations[0];

            if (Lines.Count == 0)
                AddLine();
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

    public void AddLine()
    {
        var line =
            new PurchaseOrderLineEditViewModel();

        line.PropertyChanged += Line_PropertyChanged;

        Lines.Add(line);

        RefreshTotal();
    }

    public void RemoveLine(
        PurchaseOrderLineEditViewModel line)
    {
        if (Lines.Count <= 1)
            return;

        line.PropertyChanged -= Line_PropertyChanged;

        Lines.Remove(line);

        RefreshTotal();
    }

    private void Line_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(
                PurchaseOrderLineEditViewModel.Quantity) ||
            e.PropertyName == nameof(
                PurchaseOrderLineEditViewModel.UnitCost))
        {
            RefreshTotal();
        }
    }

    private void RefreshTotal()
    {
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(TotalAmountDisplay));
    }

    public async Task<Guid?> SaveAsync()
    {
        try
        {
            IsSaving = true;
            ErrorMessage = null;

            if (SelectedSupplier is null)
            {
                ErrorMessage =
                    "Please select a supplier.";

                return null;
            }

            if (SelectedLocation is null)
            {
                ErrorMessage =
                    "Please select a location.";

                return null;
            }

            if (Lines.Count == 0)
            {
                ErrorMessage =
                    "Add at least one product.";

                return null;
            }

            foreach (var line in Lines)
            {
                if (line.SelectedProduct is null)
                {
                    ErrorMessage =
                        "Select a product for every line.";

                    return null;
                }

                if (line.Quantity <= 0)
                {
                    ErrorMessage =
                        "Quantity must be greater than zero.";

                    return null;
                }

                if (line.UnitCost < 0)
                {
                    ErrorMessage =
                        "Unit cost cannot be negative.";

                    return null;
                }

                if (line.WarrantyMonths < 0)
                {
                    ErrorMessage =
                        "Warranty cannot be negative.";

                    return null;
                }
            }

            var request =
                new CreatePurchaseOrderRequest
                {
                    SupplierId =
                        SelectedSupplier.Id,

                    LocationId =
                        SelectedLocation.Id,

                    ExpectedDate =
                        ExpectedDate?.UtcDateTime,

                    Notes = Notes,

                    Lines = Lines
                        .Select(x =>
                            new CreatePurchaseOrderLineRequest
                            {
                                ProductVariantId =
                                    x.SelectedProduct!
                                        .ProductVariantId,

                                Quantity =
                                    Convert.ToDecimal(
                                        x.Quantity),

                                UnitCost =
                                    Convert.ToDecimal(
                                        x.UnitCost),

                                WarrantyMonths =
                                    x.WarrantyMonths > 0
                                        ? Convert.ToInt32(
                                            x.WarrantyMonths)
                                        : null
                            })
                        .ToList()
                };

            return await _purchaseOrderService
                .CreateAsync(request);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            IsSaving = false;
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