using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class SalesQuoteHistoryViewModel :
    INotifyPropertyChanged
{
    private readonly ISalesQuoteService
        _salesQuoteService;

    private string _searchText =
        string.Empty;

    private bool _isLoading;

    private string? _errorMessage;

    private SalesQuoteHistoryItemViewModel?
        _selectedQuote;

    // =========================================================
    // Collections
    // =========================================================

    public ObservableCollection<
        SalesQuoteHistoryItemViewModel>
        Quotes
    { get; } = new();

    // =========================================================
    // Search
    // =========================================================

    public string SearchText
    {
        get => _searchText;

        set
        {
            if (_searchText == value)
                return;

            _searchText = value;

            OnPropertyChanged();
        }
    }

    // =========================================================
    // Selection
    // =========================================================

    public SalesQuoteHistoryItemViewModel?
        SelectedQuote
    {
        get => _selectedQuote;

        set
        {
            if (_selectedQuote == value)
                return;

            _selectedQuote = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(HasSelectedQuote));
        }
    }

    public bool HasSelectedQuote =>
        SelectedQuote is not null;

    // =========================================================
    // UI state
    // =========================================================

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

    public string? ErrorMessage
    {
        get => _errorMessage;

        private set
        {
            if (_errorMessage == value)
                return;

            _errorMessage = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(HasError));
        }
    }

    public bool HasError =>
        !string.IsNullOrWhiteSpace(
            ErrorMessage);

    public bool HasQuotes =>
        Quotes.Count > 0;

    public bool HasNoQuotes =>
        !IsLoading &&
        Quotes.Count == 0;

    // =========================================================
    // Constructor
    // =========================================================

    public SalesQuoteHistoryViewModel(
        ISalesQuoteService salesQuoteService)
    {
        _salesQuoteService =
            salesQuoteService;
    }

    // =========================================================
    // Load
    // =========================================================

    public async Task LoadAsync()
    {
        await LoadQuotesAsync(
            SearchText);
    }

    public async Task SearchAsync()
    {
        await LoadQuotesAsync(
            SearchText);
    }

    public async Task ClearSearchAsync()
    {
        SearchText =
            string.Empty;

        await LoadQuotesAsync(
            null);
    }

    private async Task LoadQuotesAsync(
        string? search)
    {
        try
        {
            IsLoading = true;

            ErrorMessage = null;

            SelectedQuote = null;

            var quotes =
                await _salesQuoteService
                    .GetHistoryAsync(
                        string.IsNullOrWhiteSpace(search)
                            ? null
                            : search.Trim());

            Quotes.Clear();

            foreach (var quote in quotes)
            {
                Quotes.Add(
                    new SalesQuoteHistoryItemViewModel(
                        quote));
            }

            RefreshCollectionState();
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;

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
        OnPropertyChanged(
            nameof(HasQuotes));

        OnPropertyChanged(
            nameof(HasNoQuotes));
    }

    // =========================================================
    // Property changed
    // =========================================================

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


// =============================================================
// DISPLAY ITEM
// =============================================================

public class SalesQuoteHistoryItemViewModel
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

    public SalesQuoteHistoryItemViewModel(
        SalesQuoteHistoryDto quote)
    {
        Id =
            quote.Id;

        QuoteNumber =
            quote.QuoteNumber;

        QuoteDate =
            quote.QuoteDate;

        ValidUntilDate =
            quote.ValidUntilDate;

        CustomerName =
            quote.CustomerName;

        LocationName =
            quote.LocationName;

        Status =
            quote.Status;

        LineCount =
            quote.LineCount;

        TotalQuantity =
            quote.TotalQuantity;

        TotalAmount =
            quote.TotalAmount;

        SaleId =
            quote.SaleId;
    }

    // ---------------------------------------------------------
    // Display properties
    // ---------------------------------------------------------

    public string FormattedQuoteDate =>
        QuoteDate
            .ToLocalTime()
            .ToString(
                "dd MMM yyyy");

    /*
     * ValidUntilDate currently uses timestamptz in PostgreSQL.
     *
     * Do not call ToLocalTime() here because we are treating
     * this field as a business/calendar date.
     */
    public string FormattedValidUntilDate =>
        ValidUntilDate.HasValue
            ? ValidUntilDate.Value
                .ToString(
                    "dd MMM yyyy")
            : "No expiry";

    public string FormattedTotal =>
        $"৳{TotalAmount:N2}";

    public string FormattedQuantity =>
        TotalQuantity.ToString(
            "N3");

    public string ItemSummary =>
        $"{LineCount} item" +
        (LineCount == 1
            ? string.Empty
            : "s");

    public string StatusDisplay =>
        Status switch
        {
            SalesQuoteStatus.Draft =>
                "Draft",

            SalesQuoteStatus.Issued =>
                "Issued",

            SalesQuoteStatus.Accepted =>
                "Accepted",

            SalesQuoteStatus.ConvertedToSale =>
                "Converted",

            SalesQuoteStatus.Expired =>
                "Expired",

            SalesQuoteStatus.Cancelled =>
                "Cancelled",

            _ =>
                Status.ToString()
        };

    public string ConversionDisplay =>
        SaleId.HasValue
            ? "Sale created"
            : string.Empty;
}