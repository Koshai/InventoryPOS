using InventoryPOS.Application.DTOs.SalesReturns;
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

public sealed class SalesReturnViewModel :
    INotifyPropertyChanged
{
    private readonly ISalesReturnService
        _salesReturnService;

    private bool _isLoading;
    private bool _isSubmitting;
    private string? _errorMessage;
    private string? _reason;
    private string? _notes;
    private PaymentMethod _refundPaymentMethod =
        PaymentMethod.Cash;
    private string? _refundReferenceNumber;
    private string? _refundNotes;

    public SalesReturnViewModel(
        ISalesReturnService salesReturnService)
    {
        _salesReturnService =
            salesReturnService;
    }

    public Guid SaleId { get; private set; }

    public string InvoiceNumber { get; private set; } =
        string.Empty;

    public string CustomerName { get; private set; } =
        string.Empty;

    public DateTime SaleDate { get; private set; }

    public decimal SaleTotal { get; private set; }

    public decimal PaidAmount { get; private set; }

    public decimal DueAmount { get; private set; }

    public ObservableCollection<
        SalesReturnLineViewModel> Lines
    { get; } =
            new();

    public IReadOnlyList<PaymentMethod>
        RefundPaymentMethods
    { get; } =
            Enum.GetValues<PaymentMethod>();

    public PaymentMethod RefundPaymentMethod
    {
        get => _refundPaymentMethod;

        set
        {
            if (_refundPaymentMethod == value)
                return;

            _refundPaymentMethod = value;

            OnPropertyChanged();
            OnPropertyChanged(
                nameof(IsRefundReferenceRequired));
        }
    }

    public string? RefundReferenceNumber
    {
        get => _refundReferenceNumber;

        set
        {
            if (_refundReferenceNumber == value)
                return;

            _refundReferenceNumber = value;

            OnPropertyChanged();
        }
    }

    public string? RefundNotes
    {
        get => _refundNotes;

        set
        {
            if (_refundNotes == value)
                return;

            _refundNotes = value;

            OnPropertyChanged();
        }
    }

    public string? Reason
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
            OnPropertyChanged(
                nameof(CanSubmit));
        }
    }

    public bool IsSubmitting
    {
        get => _isSubmitting;

        private set
        {
            if (_isSubmitting == value)
                return;

            _isSubmitting = value;

            OnPropertyChanged();
            OnPropertyChanged(
                nameof(CanSubmit));
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

    public decimal EstimatedReturnAmount =>
        Lines.Sum(x =>
            x.EstimatedReturnAmount);

    /*
     * This is only a UI estimate.
     *
     * SalesReturnService remains authoritative.
     */
    public decimal EstimatedCreditAdjustment =>
        Math.Min(
            DueAmount,
            EstimatedReturnAmount);

    public decimal EstimatedRefundableAmount =>
        Math.Max(
            0m,
            EstimatedReturnAmount -
            EstimatedCreditAdjustment);

    public bool HasRefund =>
        EstimatedRefundableAmount > 0;

    public bool IsRefundReferenceRequired =>
        HasRefund &&
        RefundPaymentMethod !=
            PaymentMethod.Cash;

    public bool HasSelectedLines =>
        Lines.Any(x =>
            x.ReturnQuantity > 0);

    public bool CanSubmit =>
        !IsLoading &&
        !IsSubmitting &&
        HasSelectedLines;

    public string FormattedSaleDate =>
        SaleDate.ToString(
            "dd MMM yyyy");

    public string FormattedSaleTotal =>
        $"৳{SaleTotal:N2}";

    public string FormattedPaidAmount =>
        $"৳{PaidAmount:N2}";

    public string FormattedDueAmount =>
        $"৳{DueAmount:N2}";

    public string FormattedEstimatedReturnAmount =>
        $"৳{EstimatedReturnAmount:N2}";

    public string FormattedEstimatedCreditAdjustment =>
        $"৳{EstimatedCreditAdjustment:N2}";

    public string FormattedEstimatedRefundableAmount =>
        $"৳{EstimatedRefundableAmount:N2}";

    // ============================================================
    // LOAD
    // ============================================================

    public async Task LoadAsync(
        Guid saleId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            Lines.Clear();

            var sale =
                await _salesReturnService
                    .GetReturnableSaleAsync(
                        saleId);

            if (sale is null)
            {
                ErrorMessage =
                    "Sale was not found.";

                return;
            }

            SaleId =
                sale.SaleId;

            InvoiceNumber =
                sale.InvoiceNumber;

            CustomerName =
                sale.CustomerName;

            SaleDate =
                sale.SaleDate;

            SaleTotal =
                sale.TotalAmount;

            PaidAmount =
                sale.PaidAmount;

            DueAmount =
                sale.DueAmount;

            foreach (var line
                     in sale.Lines)
            {
                var item =
                    new SalesReturnLineViewModel
                    {
                        SaleLineId =
                            line.SaleLineId,

                        ProductVariantId =
                            line.ProductVariantId,

                        ProductName =
                            line.ProductName,

                        SKU =
                            line.Sku,

                        SoldQuantity =
                            line.SoldQuantity,

                        ReturnedQuantity =
                            line.ReturnedQuantity,

                        UnitPrice =
                            line.UnitPrice,

                        OriginalLineTotal =
                            line.OriginalLineTotal,

                        OriginalRefundableTotal =
                            line.OriginalRefundableTotal,

                        PreviouslyReturnedTotal =
                            line.PreviouslyReturnedTotal
                    };

                item.ReturnQuantityChanged +=
                    Line_ReturnQuantityChanged;

                Lines.Add(
                    item);
            }

            RaiseHeaderProperties();
            RaiseTotals();
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ============================================================
    // CREATE RETURN
    // ============================================================

    public async Task<CreateSalesReturnResultDto?>
        SubmitAsync()
    {
        try
        {
            ErrorMessage = null;

            var selectedLines =
                Lines
                    .Where(x =>
                        x.ReturnQuantity > 0)
                    .ToList();

            if (selectedLines.Count == 0)
            {
                ErrorMessage =
                    "Select at least one item to return.";

                return null;
            }

            foreach (var line
                     in selectedLines)
            {
                var quantity =
                    Convert.ToDecimal(
                        line.ReturnQuantity);

                if (quantity <= 0 ||
                    quantity >
                        line.ReturnableQuantity)
                {
                    ErrorMessage =
                        $"Invalid return quantity for {line.ProductName}.";

                    return null;
                }
            }

            if (IsRefundReferenceRequired &&
                string.IsNullOrWhiteSpace(
                    RefundReferenceNumber))
            {
                ErrorMessage =
                    "A refund reference is required for this refund method.";

                return null;
            }

            IsSubmitting = true;

            /*
             * For the MVP entry screen we settle the complete
             * currently estimated refundable portion.
             *
             * The service validates this again authoritatively.
             */
            var request =
                new CreateSalesReturnRequest
                {
                    SaleId =
                        SaleId,

                    ReturnDate =
                        DateTime.UtcNow,

                    Reason =
                        Clean(Reason),

                    Notes =
                        Clean(Notes),

                    RefundAmount =
                        EstimatedRefundableAmount,

                    RefundPaymentMethod =
                        HasRefund
                            ? RefundPaymentMethod
                            : null,

                    RefundReferenceNumber =
                        HasRefund
                            ? Clean(
                                RefundReferenceNumber)
                            : null,

                    RefundNotes =
                        HasRefund
                            ? Clean(
                                RefundNotes)
                            : null,

                    Lines =
                        selectedLines
                            .Select(x =>
                                new CreateSalesReturnLineRequest
                                {
                                    SaleLineId =
                                        x.SaleLineId,

                                    Quantity =
                                        Convert.ToDecimal(
                                            x.ReturnQuantity),

                                    IsRestockable =
                                        x.IsRestockable,

                                    Reason =
                                        Clean(
                                            x.Reason)
                                })
                            .ToList()
                };

            return await _salesReturnService
                .CreateReturnAsync(
                    request);
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;

            return null;
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    // ============================================================
    // TOTALS
    // ============================================================

    private void Line_ReturnQuantityChanged(
        object? sender,
        EventArgs e)
    {
        RaiseTotals();
    }

    private void RaiseTotals()
    {
        OnPropertyChanged(
            nameof(EstimatedReturnAmount));

        OnPropertyChanged(
            nameof(EstimatedCreditAdjustment));

        OnPropertyChanged(
            nameof(EstimatedRefundableAmount));

        OnPropertyChanged(
            nameof(FormattedEstimatedReturnAmount));

        OnPropertyChanged(
            nameof(FormattedEstimatedCreditAdjustment));

        OnPropertyChanged(
            nameof(FormattedEstimatedRefundableAmount));

        OnPropertyChanged(
            nameof(HasRefund));

        OnPropertyChanged(
            nameof(IsRefundReferenceRequired));

        OnPropertyChanged(
            nameof(HasSelectedLines));

        OnPropertyChanged(
            nameof(CanSubmit));
    }

    private void RaiseHeaderProperties()
    {
        OnPropertyChanged(
            nameof(InvoiceNumber));

        OnPropertyChanged(
            nameof(CustomerName));

        OnPropertyChanged(
            nameof(SaleDate));

        OnPropertyChanged(
            nameof(SaleTotal));

        OnPropertyChanged(
            nameof(PaidAmount));

        OnPropertyChanged(
            nameof(DueAmount));

        OnPropertyChanged(
            nameof(FormattedSaleDate));

        OnPropertyChanged(
            nameof(FormattedSaleTotal));

        OnPropertyChanged(
            nameof(FormattedPaidAmount));

        OnPropertyChanged(
            nameof(FormattedDueAmount));
    }

    private static string? Clean(
        string? value)
    {
        return string.IsNullOrWhiteSpace(
                value)
            ? null
            : value.Trim();
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