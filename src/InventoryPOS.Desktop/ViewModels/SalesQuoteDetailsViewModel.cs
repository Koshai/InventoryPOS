using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class SalesQuoteDetailsViewModel :
    INotifyPropertyChanged
{
    private readonly ISalesQuoteService
        _salesQuoteService;

    private SalesQuoteDetailsDto?
        _quote;

    private bool _isLoading;

    private string? _errorMessage;

    public bool CanIssue =>
    Quote?.Status ==
    SalesQuoteStatus.Draft;

    public bool CanAccept =>
        Quote?.Status ==
        SalesQuoteStatus.Issued;

    public bool CanCancel =>
        Quote?.Status ==
            SalesQuoteStatus.Draft ||
        Quote?.Status ==
            SalesQuoteStatus.Issued ||
        Quote?.Status ==
            SalesQuoteStatus.Accepted;

    public bool CanExpire =>
        Quote?.Status ==
            SalesQuoteStatus.Issued &&
        Quote.ValidUntilDate.HasValue &&
        Quote.ValidUntilDate.Value.Date <
            DateTime.UtcNow.Date;

    public bool CanLoadIntoPos =>
        Quote?.Status ==
        SalesQuoteStatus.Accepted;

    public bool IsTerminal =>
        Quote?.Status ==
            SalesQuoteStatus.ConvertedToSale ||
        Quote?.Status ==
            SalesQuoteStatus.Cancelled ||
        Quote?.Status ==
            SalesQuoteStatus.Expired;

    // =========================================================
    // Quote
    // =========================================================

    public SalesQuoteDetailsDto? Quote
    {
        get => _quote;

        private set
        {
            if (_quote == value)
                return;

            _quote = value;

            OnPropertyChanged();

            RefreshDisplayProperties();
        }
    }

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

    public bool HasQuote =>
        Quote is not null;

    // =========================================================
    // Header display
    // =========================================================

    public string QuoteNumber =>
        Quote?.QuoteNumber ??
        string.Empty;

    public string CustomerName =>
        Quote?.CustomerName ??
        string.Empty;

    public string CustomerPhone =>
        string.IsNullOrWhiteSpace(
            Quote?.CustomerPhone)
            ? "—"
            : Quote!.CustomerPhone!;

    public string CustomerEmail =>
        string.IsNullOrWhiteSpace(
            Quote?.CustomerEmail)
            ? "—"
            : Quote!.CustomerEmail!;

    public string LocationName =>
        Quote?.LocationName ??
        string.Empty;

    public string FormattedQuoteDate =>
        Quote is null
            ? string.Empty
            : Quote.QuoteDate
                .ToLocalTime()
                .ToString(
                    "dd MMM yyyy");

    /*
     * ValidUntilDate currently uses timestamptz,
     * but semantically represents a business date.
     */
    public string FormattedValidUntilDate =>
        Quote?.ValidUntilDate is DateTime date
            ? date.ToString(
                "dd MMM yyyy")
            : "No expiry";

    public string StatusDisplay =>
        Quote is null
            ? string.Empty
            : Quote.Status switch
            {
                SalesQuoteStatus.Draft =>
                    "Draft",

                SalesQuoteStatus.Issued =>
                    "Issued",

                SalesQuoteStatus.Accepted =>
                    "Accepted",

                SalesQuoteStatus.ConvertedToSale =>
                    "Converted to Sale",

                SalesQuoteStatus.Expired =>
                    "Expired",

                SalesQuoteStatus.Cancelled =>
                    "Cancelled",

                _ =>
                    Quote.Status.ToString()
            };

    // =========================================================
    // Totals
    // =========================================================

    public string FormattedSubtotal =>
        Quote is null
            ? "৳0.00"
            : $"৳{Quote.Subtotal:N2}";

    public string FormattedDiscount =>
        Quote is null
            ? "৳0.00"
            : $"৳{Quote.DiscountAmount:N2}";

    public string FormattedTax =>
        Quote is null
            ? "৳0.00"
            : $"৳{Quote.TaxAmount:N2}";

    public string FormattedTotal =>
        Quote is null
            ? "৳0.00"
            : $"৳{Quote.TotalAmount:N2}";

    // =========================================================
    // Notes / terms
    // =========================================================

    public string NotesDisplay =>
        string.IsNullOrWhiteSpace(
            Quote?.Notes)
            ? "No notes."
            : Quote!.Notes!;

    public string TermsDisplay =>
        string.IsNullOrWhiteSpace(
            Quote?.Terms)
            ? "No terms specified."
            : Quote!.Terms!;

    // =========================================================
    // Sale
    // =========================================================

    public bool HasLinkedSale =>
        Quote?.SaleId is not null;

    public string LinkedSaleDisplay =>
        HasLinkedSale
            ? $"Invoice: {Quote!.InvoiceNumber}"
            : "Not converted to a sale";

    // =========================================================
    // Constructor
    // =========================================================

    public SalesQuoteDetailsViewModel(
        ISalesQuoteService salesQuoteService)
    {
        _salesQuoteService =
            salesQuoteService;
    }

    // =========================================================
    // Load
    // =========================================================

    public async Task LoadAsync(
        Guid salesQuoteId)
    {
        try
        {
            IsLoading = true;

            ErrorMessage = null;

            Quote =
                await _salesQuoteService
                    .GetByIdAsync(
                        salesQuoteId);

            if (Quote is null)
            {
                ErrorMessage =
                    "The quotation could not be found.";
            }
        }
        catch (Exception ex)
        {
            Quote = null;

            ErrorMessage =
                ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // =========================================================
    // Refresh display
    // =========================================================

    private void RefreshDisplayProperties()
    {
        OnPropertyChanged(
            nameof(HasQuote));

        OnPropertyChanged(
            nameof(QuoteNumber));

        OnPropertyChanged(
            nameof(CustomerName));

        OnPropertyChanged(
            nameof(CustomerPhone));

        OnPropertyChanged(
            nameof(CustomerEmail));

        OnPropertyChanged(
            nameof(LocationName));

        OnPropertyChanged(
            nameof(FormattedQuoteDate));

        OnPropertyChanged(
            nameof(FormattedValidUntilDate));

        OnPropertyChanged(
            nameof(StatusDisplay));

        OnPropertyChanged(
            nameof(FormattedSubtotal));

        OnPropertyChanged(
            nameof(FormattedDiscount));

        OnPropertyChanged(
            nameof(FormattedTax));

        OnPropertyChanged(
            nameof(FormattedTotal));

        OnPropertyChanged(
            nameof(NotesDisplay));

        OnPropertyChanged(
            nameof(TermsDisplay));

        OnPropertyChanged(
            nameof(HasLinkedSale));

        OnPropertyChanged(
            nameof(LinkedSaleDisplay));

        RefreshActionProperties();
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

    public async Task IssueAsync()
    {
        if (Quote is null)
            return;

        await ExecuteStatusChangeAsync(
            () => _salesQuoteService
                .IssueAsync(
                    Quote.Id));
    }


    public async Task AcceptAsync()
    {
        if (Quote is null)
            return;

        await ExecuteStatusChangeAsync(
            () => _salesQuoteService
                .AcceptAsync(
                    Quote.Id));
    }


    public async Task CancelAsync()
    {
        if (Quote is null)
            return;

        await ExecuteStatusChangeAsync(
            () => _salesQuoteService
                .CancelAsync(
                    Quote.Id));
    }


    public async Task ExpireAsync()
    {
        if (Quote is null)
            return;

        await ExecuteStatusChangeAsync(
            () => _salesQuoteService
                .ExpireAsync(
                    Quote.Id));
    }

    private async Task ExecuteStatusChangeAsync(
    Func<Task> operation)
    {
        if (Quote is null)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var quoteId =
                Quote.Id;

            await operation();

            Quote =
                await _salesQuoteService
                    .GetByIdAsync(
                        quoteId);

            if (Quote is null)
            {
                ErrorMessage =
                    "The quotation could not be reloaded.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;
        }
        finally
        {
            IsLoading = false;

            RefreshActionProperties();
        }
    }

    private void RefreshActionProperties()
    {
        OnPropertyChanged(
            nameof(CanIssue));

        OnPropertyChanged(
            nameof(CanAccept));

        OnPropertyChanged(
            nameof(CanCancel));

        OnPropertyChanged(
            nameof(CanExpire));

        OnPropertyChanged(
            nameof(CanLoadIntoPos));

        OnPropertyChanged(
            nameof(IsTerminal));
    }
}