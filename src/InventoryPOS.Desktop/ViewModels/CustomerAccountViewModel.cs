using InventoryPOS.Application.DTOs.Customers;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public sealed class CustomerAccountViewModel : INotifyPropertyChanged
{
    private readonly ICustomerAccountService _customerAccountService;

    private Guid _customerId;
    private CustomerAccountDto? _account;

    private bool _isLoading;
    private bool _isSaving;

    private string? _errorMessage;
    private string? _successMessage;

    private double _paymentAmount;
    private PaymentMethod _selectedPaymentMethod =
        PaymentMethod.Cash;

    private string? _referenceNumber;
    private string? _paymentNotes;

    private CustomerOutstandingSaleDto? _selectedInvoice;

    public CustomerAccountViewModel(
        ICustomerAccountService customerAccountService)
    {
        _customerAccountService =
            customerAccountService;

        PaymentMethods =
            Enum.GetValues<PaymentMethod>();
    }

    // ============================================================
    // ACCOUNT
    // ============================================================

    public CustomerAccountDto? Account
    {
        get => _account;
        private set
        {
            _account = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CustomerName));
            OnPropertyChanged(nameof(CustomerCode));
            OnPropertyChanged(nameof(Phone));
            OnPropertyChanged(nameof(FormattedCreditLimit));
            OnPropertyChanged(nameof(FormattedOutstanding));
            OnPropertyChanged(nameof(FormattedAvailableCredit));
            OnPropertyChanged(nameof(HasOutstandingBalance));
        }
    }

    public string CustomerName =>
        Account?.CustomerName ?? string.Empty;

    public string CustomerCode =>
        Account?.CustomerCode ?? string.Empty;

    public string Phone =>
        string.IsNullOrWhiteSpace(Account?.Phone)
            ? "No phone"
            : Account!.Phone!;

    public string FormattedCreditLimit =>
        $"৳{Account?.CreditLimit ?? 0:N2}";

    public string FormattedOutstanding =>
        $"৳{Account?.OutstandingBalance ?? 0:N2}";

    public string FormattedAvailableCredit =>
        $"৳{Account?.AvailableCredit ?? 0:N2}";

    public bool HasOutstandingBalance =>
        (Account?.OutstandingBalance ?? 0) > 0;

    // ============================================================
    // PAYMENT
    // ============================================================

    public Array PaymentMethods { get; }

    public double PaymentAmount
    {
        get => _paymentAmount;
        set
        {
            if (Math.Abs(_paymentAmount - value) < 0.001)
                return;

            _paymentAmount = value;
            OnPropertyChanged();
        }
    }

    public PaymentMethod SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set
        {
            if (_selectedPaymentMethod == value)
                return;

            _selectedPaymentMethod = value;

            if (value == PaymentMethod.Cash)
                ReferenceNumber = null;

            OnPropertyChanged();
            OnPropertyChanged(nameof(RequiresReference));
            OnPropertyChanged(nameof(ReferenceLabel));
        }
    }

    public bool RequiresReference =>
        SelectedPaymentMethod != PaymentMethod.Cash;

    public string ReferenceLabel =>
        SelectedPaymentMethod switch
        {
            PaymentMethod.Bkash =>
                "bKash Transaction ID",

            PaymentMethod.Nagad =>
                "Nagad Transaction ID",

            PaymentMethod.Rocket =>
                "Rocket Transaction ID",

            PaymentMethod.Card =>
                "Card Reference",

            PaymentMethod.BankTransfer =>
                "Bank Reference",

            PaymentMethod.Other =>
                "Payment Reference",

            _ =>
                "Reference"
        };

    public string? ReferenceNumber
    {
        get => _referenceNumber;
        set
        {
            if (_referenceNumber == value)
                return;

            _referenceNumber = value;
            OnPropertyChanged();
        }
    }

    public string? PaymentNotes
    {
        get => _paymentNotes;
        set
        {
            if (_paymentNotes == value)
                return;

            _paymentNotes = value;
            OnPropertyChanged();
        }
    }

    public CustomerOutstandingSaleDto? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (_selectedInvoice == value)
                return;

            _selectedInvoice = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedInvoice));
        }
    }

    public bool HasSelectedInvoice =>
        SelectedInvoice is not null;

    // ============================================================
    // STATE
    // ============================================================

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            _isLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBusy));
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            _isSaving = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBusy));
        }
    }

    public bool IsBusy =>
        IsLoading || IsSaving;

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError =>
        !string.IsNullOrWhiteSpace(ErrorMessage);

    public string? SuccessMessage
    {
        get => _successMessage;
        private set
        {
            _successMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSuccess));
        }
    }

    public bool HasSuccess =>
        !string.IsNullOrWhiteSpace(SuccessMessage);

    // ============================================================
    // LOAD
    // ============================================================

    public async Task LoadAsync(Guid customerId)
    {
        try
        {
            IsLoading = true;

            ErrorMessage = null;
            SuccessMessage = null;

            _customerId = customerId;

            Account =
                await _customerAccountService
                    .GetAccountAsync(customerId);

            if (Account is null)
            {
                ErrorMessage =
                    "Customer account was not found.";
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

    // ============================================================
    // RECEIVE GENERAL ACCOUNT PAYMENT
    // ============================================================

    public async Task<bool> ReceiveAccountPaymentAsync()
    {
        if (!ValidatePayment())
            return false;

        if (Account is null)
        {
            ErrorMessage =
                "Customer account is not loaded.";

            return false;
        }

        var amount = (decimal)PaymentAmount;

        if (amount > Account.OutstandingBalance)
        {
            ErrorMessage =
                $"Payment cannot exceed the outstanding balance of ৳{Account.OutstandingBalance:N2}.";

            return false;
        }

        try
        {
            IsSaving = true;

            ErrorMessage = null;
            SuccessMessage = null;

            var request =
                CreatePaymentRequest(amount);

            var result =
                await _customerAccountService
                    .ReceiveAccountPaymentAsync(
                        request);

            SuccessMessage =
                $"Payment of ৳{result.AmountApplied:N2} received successfully.";

            ClearPaymentForm();

            await ReloadAsync();

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

    // ============================================================
    // RECEIVE PAYMENT AGAINST SELECTED INVOICE
    // ============================================================

    public async Task<bool> ReceiveInvoicePaymentAsync()
    {
        if (SelectedInvoice is null)
        {
            ErrorMessage =
                "Select an outstanding invoice first.";

            return false;
        }

        if (!ValidatePayment())
            return false;

        var amount =
            (decimal)PaymentAmount;

        if (amount > SelectedInvoice.DueAmount)
        {
            ErrorMessage =
                $"Payment cannot exceed the selected invoice due amount of ৳{SelectedInvoice.DueAmount:N2}.";

            return false;
        }

        try
        {
            IsSaving = true;

            ErrorMessage = null;
            SuccessMessage = null;

            var invoiceNumber =
                SelectedInvoice.InvoiceNumber;

            var request =
                CreatePaymentRequest(amount);

            var result =
                await _customerAccountService
                    .ReceiveInvoicePaymentAsync(
                        SelectedInvoice.SaleId,
                        request);

            SuccessMessage =
                $"Payment of ৳{result.AmountApplied:N2} applied to {invoiceNumber}.";

            SelectedInvoice = null;

            ClearPaymentForm();

            await ReloadAsync();

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

    // ============================================================
    // HELPERS
    // ============================================================

    public void PrepareInvoicePayment(
        CustomerOutstandingSaleDto invoice)
    {
        SelectedInvoice = invoice;

        PaymentAmount =
            (double)invoice.DueAmount;

        ErrorMessage = null;
        SuccessMessage = null;
    }

    public void PrepareAccountPayment()
    {
        SelectedInvoice = null;

        PaymentAmount =
            (double)(Account?.OutstandingBalance ?? 0);

        ErrorMessage = null;
        SuccessMessage = null;
    }

    public void CancelPayment()
    {
        SelectedInvoice = null;

        ClearPaymentForm();

        ErrorMessage = null;
    }

    private ReceiveCustomerPaymentRequest
        CreatePaymentRequest(decimal amount)
    {
        return new ReceiveCustomerPaymentRequest
        {
            CustomerId = _customerId,
            Amount = amount,

            PaymentMethod =
                SelectedPaymentMethod,

            PaymentDate =
                DateTime.UtcNow,

            ReferenceNumber =
                ReferenceNumber,

            Notes =
                PaymentNotes
        };
    }

    private bool ValidatePayment()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (Account is null)
        {
            ErrorMessage =
                "Customer account is not loaded.";

            return false;
        }

        if (double.IsNaN(PaymentAmount) ||
            double.IsInfinity(PaymentAmount) ||
            PaymentAmount <= 0)
        {
            ErrorMessage =
                "Enter a payment amount greater than zero.";

            return false;
        }

        if (RequiresReference &&
            string.IsNullOrWhiteSpace(
                ReferenceNumber))
        {
            ErrorMessage =
                $"{ReferenceLabel} is required.";

            return false;
        }

        return true;
    }

    private async Task ReloadAsync()
    {
        Account =
            await _customerAccountService
                .GetAccountAsync(_customerId);

        if (Account is null)
        {
            throw new InvalidOperationException(
                "Customer account could not be reloaded.");
        }
    }

    private void ClearPaymentForm()
    {
        PaymentAmount = 0;

        SelectedPaymentMethod =
            PaymentMethod.Cash;

        ReferenceNumber = null;
        PaymentNotes = null;
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