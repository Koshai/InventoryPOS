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

public class CreatePurchaseRequisitionViewModel
    : INotifyPropertyChanged
{
    private readonly IPurchaseRequisitionService
        _purchaseRequisitionService;

    private readonly IPurchaseOrderService
        _purchaseOrderService;

    private SupplierLookupDto? _selectedSupplier;
    private LocationLookupDto? _selectedLocation;

    private DateTimeOffset _requisitionDate =
        DateTimeOffset.Now;

    private DateTimeOffset? _requiredByDate;
    private DateTimeOffset? _validUntilDate;

    private string? _subject;
    private string? _notes;

    private string? _terms =
        "Please provide pricing and expected delivery time.";

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

    public ObservableCollection<PurchaseRequisitionLineEditViewModel>
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

    public DateTimeOffset RequisitionDate
    {
        get => _requisitionDate;
        set
        {
            if (_requisitionDate == value)
                return;

            _requisitionDate = value;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset? RequiredByDate
    {
        get => _requiredByDate;
        set
        {
            if (_requiredByDate == value)
                return;

            _requiredByDate = value;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset? ValidUntilDate
    {
        get => _validUntilDate;
        set
        {
            if (_validUntilDate == value)
                return;

            _validUntilDate = value;
            OnPropertyChanged();
        }
    }

    public string? Subject
    {
        get => _subject;
        set
        {
            if (_subject == value)
                return;

            _subject = value;
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

    public string? Terms
    {
        get => _terms;
        set
        {
            if (_terms == value)
                return;

            _terms = value;
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
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError =>
        !string.IsNullOrWhiteSpace(ErrorMessage);

    public decimal EstimatedTotal =>
        Lines.Sum(x => x.EstimatedLineTotal);

    public string EstimatedTotalDisplay =>
        $"৳{EstimatedTotal:N2}";

    public CreatePurchaseRequisitionViewModel(
        IPurchaseRequisitionService purchaseRequisitionService,
        IPurchaseOrderService purchaseOrderService)
    {
        _purchaseRequisitionService =
            purchaseRequisitionService;

        _purchaseOrderService =
            purchaseOrderService;

        Lines.CollectionChanged += (_, _) =>
        {
            RefreshTotal();
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

            // Sequential intentionally because these services
            // may share the same scoped DbContext.
            var suppliers =
                await _purchaseOrderService
                    .GetSuppliersAsync();

            foreach (var supplier in suppliers)
                Suppliers.Add(supplier);

            var locations =
                await _purchaseOrderService
                    .GetLocationsAsync();

            foreach (var location in locations)
                Locations.Add(location);

            var products =
                await _purchaseOrderService
                    .GetProductVariantsAsync();

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
            new PurchaseRequisitionLineEditViewModel();

        line.PropertyChanged +=
            Line_PropertyChanged;

        Lines.Add(line);

        RefreshTotal();
    }

    public void RemoveLine(
        PurchaseRequisitionLineEditViewModel line)
    {
        if (Lines.Count <= 1)
            return;

        line.PropertyChanged -=
            Line_PropertyChanged;

        Lines.Remove(line);

        RefreshTotal();
    }

    private void Line_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
                nameof(PurchaseRequisitionLineEditViewModel.Quantity) ||
            e.PropertyName ==
                nameof(PurchaseRequisitionLineEditViewModel.EstimatedUnitCost) ||
            e.PropertyName ==
                nameof(PurchaseRequisitionLineEditViewModel.HasEstimatedCost))
        {
            RefreshTotal();
        }
    }

    private void RefreshTotal()
    {
        OnPropertyChanged(
            nameof(EstimatedTotal));

        OnPropertyChanged(
            nameof(EstimatedTotalDisplay));
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
                        "Requested quantity must be greater than zero.";

                    return null;
                }

                if (line.HasEstimatedCost &&
                    line.EstimatedUnitCost < 0)
                {
                    ErrorMessage =
                        "Estimated unit cost cannot be negative.";

                    return null;
                }
            }

            var duplicateProduct =
                Lines
                    .Where(x =>
                        x.SelectedProduct is not null)
                    .GroupBy(x =>
                        x.SelectedProduct!.ProductVariantId)
                    .FirstOrDefault(x =>
                        x.Count() > 1);

            if (duplicateProduct is not null)
            {
                ErrorMessage =
                    "The same product cannot appear more than once.";

                return null;
            }

            if (RequiredByDate.HasValue &&
                RequiredByDate.Value.Date <
                RequisitionDate.Date)
            {
                ErrorMessage =
                    "Required-by date cannot be earlier than the requisition date.";

                return null;
            }

            if (ValidUntilDate.HasValue &&
                ValidUntilDate.Value.Date <
                RequisitionDate.Date)
            {
                ErrorMessage =
                    "Valid-until date cannot be earlier than the requisition date.";

                return null;
            }

            var request =
                new CreatePurchaseRequisitionRequest
                {
                    SupplierId =
                        SelectedSupplier.Id,

                    LocationId =
                        SelectedLocation.Id,

                    RequisitionDate =
                        RequisitionDate.UtcDateTime,

                    RequiredByDate =
                        RequiredByDate?.UtcDateTime,

                    ValidUntilDate =
                        ValidUntilDate?.UtcDateTime,

                    Subject =
                        Subject,

                    Notes =
                        Notes,

                    Terms =
                        Terms,

                    Lines =
                        Lines.Select(x =>
                            new CreatePurchaseRequisitionLineRequest
                            {
                                ProductVariantId =
                                    x.SelectedProduct!
                                        .ProductVariantId,

                                RequestedQuantity =
                                    Convert.ToDecimal(
                                        x.Quantity),

                                EstimatedUnitCost =
                                    x.HasEstimatedCost
                                        ? Convert.ToDecimal(
                                            x.EstimatedUnitCost)
                                        : null,

                                Description =
                                    x.Description,

                                Notes =
                                    x.Notes
                            })
                        .ToList()
                };

            var result =
                await _purchaseRequisitionService
                    .CreateAsync(request);

            return result.PurchaseRequisitionId;
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
            new PropertyChangedEventArgs(
                propertyName));
    }
}