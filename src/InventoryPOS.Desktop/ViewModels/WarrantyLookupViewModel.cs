
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using InventoryPOS.Application.DTOs.Warranty;
using InventoryPOS.Application.Interfaces;

namespace InventoryPOS.Desktop.ViewModels;

public sealed class WarrantyLookupViewModel : INotifyPropertyChanged
{
    private readonly IWarrantyLookupService _warrantyLookupService;

    private string _invoiceNumber = string.Empty;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    public Guid? SelectedSaleId => _result?.SaleId;
    private WarrantyLookupResultDto? _result;

    public WarrantyLookupViewModel(
        IWarrantyLookupService warrantyLookupService)
    {
        _warrantyLookupService = warrantyLookupService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<WarrantyLookupRowViewModel> Items
    { get; } = new();


    private string _invoiceSearchText = string.Empty;
    private int _currentPage = 1;
    private int _totalPages;
    private int _totalCount;

    public string TotalCountDisplay =>
    $"{TotalCount} matching invoices";

    public ObservableCollection<WarrantyInvoiceSearchDto> Invoices
    { get; } = new();

    public string InvoiceSearchText
    {
        get => _invoiceSearchText;
        set
        {
            if (_invoiceSearchText == value) return;
            _invoiceSearchText = value;
            OnPropertyChanged();
        }
    }

    public int CurrentPage
    {
        get => _currentPage;
        private set
        {
            _currentPage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PageDescription));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        private set
        {
            _totalPages = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PageDescription));
            OnPropertyChanged(nameof(CanGoNext));
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        private set
        {
            _totalCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalCountDisplay));
        }
    }

    public string PageDescription =>
        $"Page {CurrentPage} of {Math.Max(1, TotalPages)}";

    public bool CanGoPrevious =>
        !IsLoading && CurrentPage > 1;

    public bool CanGoNext =>
        !IsLoading && CurrentPage < TotalPages;

    public async Task LoadRecentInvoicesAsync()
    {
        await LoadInvoicePageAsync(1);
    }

    public async Task SearchInvoicesAsync()
    {
        await LoadInvoicePageAsync(1);
    }

    public async Task PreviousPageAsync()
    {
        if (CanGoPrevious)
            await LoadInvoicePageAsync(CurrentPage - 1);
    }

    public async Task NextPageAsync()
    {
        if (CanGoNext)
            await LoadInvoicePageAsync(CurrentPage + 1);
    }

    private async Task LoadInvoicePageAsync(int page)
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result =
                await _warrantyLookupService.SearchInvoicesAsync(
                    InvoiceSearchText, page, 25);

            Invoices.Clear();

            foreach (var invoice in result.Items)
                Invoices.Add(invoice);

            CurrentPage = result.Page;
            TotalPages = result.TotalPages;
            TotalCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Unable to search invoices: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
        }
    }

    public async Task SelectInvoiceAsync(Guid saleId)
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        ClearResult();

        try
        {
            var result =
                await _warrantyLookupService.GetBySaleIdAsync(saleId);

            if (result is null)
            {
                ErrorMessage = "The selected invoice was not found.";
                return;
            }

            _result = result;
            InvoiceNumber = result.InvoiceNumber;

            foreach (var item in result.Items)
                Items.Add(new WarrantyLookupRowViewModel(item));

            NotifyResultChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage =
                $"Unable to load warranty details: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }


    public string InvoiceNumber
    {
        get => _invoiceNumber;
        set
        {
            if (_invoiceNumber == value) return;
            _invoiceNumber = value;
            OnPropertyChanged();
        }
    }

    public Microsoft.UI.Xaml.Visibility ResultVisibility =>
    HasResult
        ? Microsoft.UI.Xaml.Visibility.Visible
        : Microsoft.UI.Xaml.Visibility.Collapsed;

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasResult => _result is not null;

    public string ResultInvoiceNumber =>
        _result?.InvoiceNumber ?? string.Empty;

    public string CustomerName =>
        _result?.CustomerName ?? string.Empty;

    public string CustomerPhone =>
        string.IsNullOrWhiteSpace(_result?.CustomerPhone)
            ? "Not provided"
            : _result.CustomerPhone;

    public string SaleDate =>
        _result is null
            ? string.Empty
            : _result.SaleDate.ToLocalTime().ToString("dd MMM yyyy");

    public string ItemCount =>
        $"{Items.Count} product line(s)";

    public async Task SearchAsync()
    {
        if (IsLoading) return;

        ErrorMessage = string.Empty;
        ClearResult();

        if (string.IsNullOrWhiteSpace(InvoiceNumber))
        {
            ErrorMessage = "Please enter an invoice number.";
            return;
        }

        IsLoading = true;

        try
        {
            var result =
                await _warrantyLookupService.GetByInvoiceNumberAsync(
                    InvoiceNumber.Trim());

            if (result is null)
            {
                ErrorMessage = "No invoice found with that number.";
                return;
            }

            _result = result;

            foreach (var item in result.Items)
            {
                Items.Add(new WarrantyLookupRowViewModel(item));
            }

            NotifyResultChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Warranty lookup failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Clear()
    {
        if (IsLoading) return;

        InvoiceNumber = string.Empty;
        ErrorMessage = string.Empty;
        ClearResult();
    }

    private void ClearResult()
    {
        _result = null;
        Items.Clear();
        NotifyResultChanged();
    }

    private void NotifyResultChanged()
    {
        OnPropertyChanged(nameof(HasResult));
        OnPropertyChanged(nameof(ResultInvoiceNumber));
        OnPropertyChanged(nameof(CustomerName));
        OnPropertyChanged(nameof(CustomerPhone));
        OnPropertyChanged(nameof(SaleDate));
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(ResultVisibility));
        OnPropertyChanged(nameof(SelectedSaleId));
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class WarrantyLookupRowViewModel
{
    public WarrantyLookupRowViewModel(WarrantyLookupItemDto item)
    {
        SaleLineId = item.SaleLineId;
        ProductName = item.ProductName;
        SKU = item.SKU;
        Quantity = item.QuantityPurchased.ToString("0.###");
        WarrantyPeriod = item.WarrantyMonths.HasValue
            && item.WarrantyMonths.Value > 0
                ? $"{item.WarrantyMonths.Value} month(s)"
                : "None";

        WarrantyStart = FormatDate(item.WarrantyStartDate);
        WarrantyEnd = FormatDate(item.WarrantyEndDate);
        Status = item.EligibilityStatus.ToString();
        Message = item.EligibilityMessage;

        QuantityPurchased = item.QuantityPurchased;

        RemainingClaimableQuantity =
            item.RemainingClaimableQuantity;

        RemainingQuantityDisplay =
            $"Available to claim: {RemainingClaimableQuantity:0.###} " +
            $"of {QuantityPurchased:0.###}";

        CanRegister =
            item.EligibilityStatus == WarrantyEligibilityStatus.Active &&
            RemainingClaimableQuantity > 0;

        StatusBrush = item.EligibilityStatus switch
        {
            WarrantyEligibilityStatus.Active =>
                new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.Colors.ForestGreen),

            WarrantyEligibilityStatus.Expired =>
                new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.Colors.IndianRed),

            WarrantyEligibilityStatus.NotStarted =>
                new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.Colors.DarkOrange),

            _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Gray)
        };
    }

    public Guid SaleLineId { get; }
    public string ProductName { get; }
    public string SKU { get; }
    public string Quantity { get; }
    public string WarrantyPeriod { get; }
    public string WarrantyStart { get; }
    public string WarrantyEnd { get; }
    public string Status { get; }
    public string Message { get; }
    public bool CanRegister { get; }
    public decimal QuantityPurchased { get; }

    public decimal RemainingClaimableQuantity { get; }

    public string RemainingQuantityDisplay { get; }

    public Microsoft.UI.Xaml.Media.Brush StatusBrush { get; }

    private static string FormatDate(DateTime? date)
    {
        // Warranty coverage uses calendar dates. Do not shift
        // the stored date into a different timezone.
        return date?.ToString("dd MMM yyyy") ?? "—";
    }
}
