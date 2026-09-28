using InventoryPOS.Application.DTOs.SalesReturns;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public sealed class SalesReturnDetailsViewModel : INotifyPropertyChanged
{
    private readonly ISalesReturnService _salesReturnService;

    private SalesReturnDetailsDto? _return;
    private bool _isLoading;
    private string? _errorMessage;

    public SalesReturnDetailsViewModel(
        ISalesReturnService salesReturnService)
    {
        _salesReturnService = salesReturnService;
    }

    public SalesReturnDetailsDto? Return
    {
        get => _return;

        private set
        {
            if (_return == value)
                return;

            _return = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(HasReturn));
            OnPropertyChanged(nameof(ReturnNumber));
            OnPropertyChanged(nameof(InvoiceNumber));
            OnPropertyChanged(nameof(CustomerName));
            OnPropertyChanged(nameof(LocationName));
            OnPropertyChanged(nameof(ReturnDateText));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(ReturnAmountText));
            OnPropertyChanged(nameof(AppliedToBalanceText));
            OnPropertyChanged(nameof(RefundedAmountText));
            OnPropertyChanged(nameof(OriginalSaleTotalText));
            OnPropertyChanged(nameof(ReasonText));
            OnPropertyChanged(nameof(NotesText));
        }
    }

    public ObservableCollection<SalesReturnDetailsLineDto>
        Lines
    { get; } = new();

    public ObservableCollection<SalesReturnRefundDto>
        Refunds
    { get; } = new();

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
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError =>
        !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasReturn =>
        Return is not null;

    public string ReturnNumber =>
        Return?.ReturnNumber ?? string.Empty;

    public string InvoiceNumber =>
        Return?.InvoiceNumber ?? string.Empty;

    public string CustomerName =>
        Return?.CustomerName ?? string.Empty;

    public string LocationName =>
        Return?.LocationName ?? string.Empty;

    public string ReturnDateText =>
        Return is null
            ? string.Empty
            : Return.ReturnDate.ToLocalTime()
                .ToString("dd MMM yyyy, hh:mm tt");

    public string StatusText =>
        Return?.Status.ToString() ?? string.Empty;

    public string ReturnAmountText =>
        FormatMoney(Return?.ReturnAmount ?? 0m);

    public string AppliedToBalanceText =>
        FormatMoney(Return?.CreditAdjustmentAmount ?? 0m);

    public string RefundedAmountText =>
        FormatMoney(Return?.RefundedAmount ?? 0m);

    public string OriginalSaleTotalText =>
        FormatMoney(Return?.OriginalSaleTotal ?? 0m);

    public string ReasonText =>
        string.IsNullOrWhiteSpace(Return?.Reason)
            ? "—"
            : Return!.Reason!;

    public string NotesText =>
        string.IsNullOrWhiteSpace(Return?.Notes)
            ? "—"
            : Return!.Notes!;

    public async Task LoadAsync(Guid salesReturnId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var result =
                await _salesReturnService
                    .GetReturnDetailsAsync(
                        salesReturnId);

            Return = result;

            Lines.Clear();
            Refunds.Clear();

            if (result is null)
            {
                ErrorMessage =
                    "The sales return could not be found.";

                return;
            }

            foreach (var line in result.Lines)
                Lines.Add(line);

            foreach (var refund in result.Refunds)
                Refunds.Add(refund);
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

    public string GetRefundTypeText(
        SalesRefundType refundType)
    {
        return refundType switch
        {
            SalesRefundType.PaymentRefund =>
                "Payment Refund",

            SalesRefundType.CustomerCreditAdjustment =>
                "Applied to Balance",

            SalesRefundType.NoRefund =>
                "No Refund",

            _ =>
                refundType.ToString()
        };
    }

    private static string FormatMoney(decimal value)
    {
        return $"৳{value:N2}";
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