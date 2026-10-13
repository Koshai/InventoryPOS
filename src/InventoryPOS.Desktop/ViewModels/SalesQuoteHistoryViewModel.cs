using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class SalesQuoteHistoryViewModel : INotifyPropertyChanged
{
    private readonly ISalesQuoteService _salesQuoteService;
    private string _searchText = string.Empty;
    private bool _isLoading;
    private string? _errorMessage;
    private SalesQuoteHistoryItemViewModel? _selectedQuote;

    public ObservableCollection<SalesQuoteHistoryItemViewModel> Quotes { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            OnPropertyChanged();
        }
    }

    public SalesQuoteHistoryItemViewModel? SelectedQuote
    {
        get => _selectedQuote;
        set
        {
            if (_selectedQuote == value) return;
            _selectedQuote = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedQuote));
        }
    }

    public bool HasSelectedQuote => SelectedQuote is not null;

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value) return;
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasQuotes => Quotes.Count > 0;
    public bool HasNoQuotes => !IsLoading && Quotes.Count == 0;

    public SalesQuoteHistoryViewModel(ISalesQuoteService salesQuoteService)
    {
        _salesQuoteService = salesQuoteService;
    }

    public Task LoadAsync() => LoadQuotesAsync(SearchText);
    public Task SearchAsync() => LoadQuotesAsync(SearchText);

    public async Task ClearSearchAsync()
    {
        SearchText = string.Empty;
        await LoadQuotesAsync(null);
    }

    private async Task LoadQuotesAsync(string? search)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            SelectedQuote = null;

            var quotes = await _salesQuoteService.GetHistoryAsync(
                string.IsNullOrWhiteSpace(search) ? null : search.Trim());

            Quotes.Clear();
            foreach (var quote in quotes)
                Quotes.Add(new SalesQuoteHistoryItemViewModel(quote));

            RefreshCollectionState();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Quotes.Clear();
            RefreshCollectionState();
        }
        finally
        {
            IsLoading = false;
            RefreshCollectionState();
        }
    }

    private void RefreshCollectionState()
    {
        OnPropertyChanged(nameof(HasQuotes));
        OnPropertyChanged(nameof(HasNoQuotes));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SalesQuoteHistoryItemViewModel : INotifyPropertyChanged
{
    public Guid Id { get; }
    public string QuoteNumber { get; }
    public DateTime QuoteDate { get; }
    public DateTime? ValidUntilDate { get; }
    public string CustomerName { get; }
    public string LocationName { get; }
    public SalesQuoteStatus Status { get; }
    public int LineCount { get; }
    public decimal TotalQuantity { get; }
    public decimal TotalAmount { get; }
    public Guid? SaleId { get; }

    public int PricingVersion { get; }
    public decimal Subtotal { get; }
    public decimal LineDiscountAmount { get; }
    public decimal OrderDiscountAmount { get; }
    public decimal DiscountAmount { get; }
    public decimal TaxAmount { get; }
    public decimal SalesTaxRate { get; }
    public decimal TransportAmount { get; }

    private bool _isPricingExpanded;

    public bool IsPricingExpanded
    {
        get => _isPricingExpanded;
        set
        {
            if (_isPricingExpanded == value) return;
            _isPricingExpanded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PricingButtonText));
        }
    }

    public string PricingButtonText => IsPricingExpanded ? "Hide Pricing" : "Pricing";

    public void TogglePricing() => IsPricingExpanded = !IsPricingExpanded;

    public SalesQuoteHistoryItemViewModel(SalesQuoteHistoryDto quote)
    {
        Id = quote.Id;
        QuoteNumber = quote.QuoteNumber;
        QuoteDate = quote.QuoteDate;
        ValidUntilDate = quote.ValidUntilDate;
        CustomerName = quote.CustomerName;
        LocationName = quote.LocationName;
        Status = quote.Status;
        LineCount = quote.LineCount;
        TotalQuantity = quote.TotalQuantity;
        TotalAmount = quote.TotalAmount;
        SaleId = quote.SaleId;

        PricingVersion = quote.PricingVersion;
        Subtotal = quote.Subtotal;
        LineDiscountAmount = quote.LineDiscountAmount;
        OrderDiscountAmount = quote.OrderDiscountAmount;
        DiscountAmount = quote.DiscountAmount;
        TaxAmount = quote.TaxAmount;
        SalesTaxRate = quote.SalesTaxRate;
        TransportAmount = quote.TransportAmount;
    }

    public string FormattedQuoteDate => QuoteDate.ToLocalTime().ToString("dd MMM yyyy");

    // ValidUntilDate represents a business calendar date, not a local timestamp.
    public string FormattedValidUntilDate => ValidUntilDate.HasValue
        ? ValidUntilDate.Value.ToString("dd MMM yyyy")
        : "No expiry";

    public string FormattedTotal => $"৳{TotalAmount:N2}";
    public string FormattedQuantity => TotalQuantity.ToString("N3");
    public string ItemSummary => $"{LineCount} item" + (LineCount == 1 ? string.Empty : "s");

    public string StatusDisplay => Status switch
    {
        SalesQuoteStatus.Draft => "Draft",
        SalesQuoteStatus.Issued => "Issued",
        SalesQuoteStatus.Accepted => "Accepted",
        SalesQuoteStatus.ConvertedToSale => "Converted",
        SalesQuoteStatus.Expired => "Expired",
        SalesQuoteStatus.Cancelled => "Cancelled",
        _ => Status.ToString()
    };

    public string ConversionDisplay => SaleId.HasValue ? "Sale created" : string.Empty;

    public decimal OtherDiscountAmount => DiscountAmount - LineDiscountAmount - OrderDiscountAmount;
    public bool HasLineDiscount => LineDiscountAmount != 0m;
    public bool HasOrderDiscount => OrderDiscountAmount != 0m;
    public bool HasOtherDiscount => OtherDiscountAmount != 0m;
    public bool HasTax => TaxAmount != 0m;
    public bool HasTransport => TransportAmount != 0m;

    public string PricingVersionDisplay => PricingVersion >= 2
        ? "Calculator pricing"
        : "Legacy pricing";

    public string FormattedSubtotal => $"৳{Subtotal:N2}";
    public string FormattedLineDiscount => $"-৳{LineDiscountAmount:N2}";
    public string FormattedOrderDiscount => $"-৳{OrderDiscountAmount:N2}";
    public string FormattedOtherDiscount => $"-৳{OtherDiscountAmount:N2}";
    public string FormattedTax => $"+৳{TaxAmount:N2}";
    public string FormattedTransport => $"+৳{TransportAmount:N2}";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
