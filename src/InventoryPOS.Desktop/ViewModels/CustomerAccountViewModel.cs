using InventoryPOS.Application.DTOs.Customers;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Desktop.Services;
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
    private readonly IPendingCustomerPaymentJournalService _paymentJournal;
    private PendingCustomerPayment? _pendingPayment;
    private Guid _customerId;
    private CustomerAccountDto? _account;

    private bool _isLoading;
    private bool _isSaving;

    private string? _errorMessage;
    private string? _successMessage;

    private bool _paymentRecoveryBlocked;

    private double _paymentAmount;
    private PaymentMethod _selectedPaymentMethod =
        PaymentMethod.Cash;

    private string? _referenceNumber;
    private string? _paymentNotes;

    private CustomerOutstandingSaleDto? _selectedInvoice;

    public CustomerAccountViewModel(
    ICustomerAccountService customerAccountService,
    IPendingCustomerPaymentJournalService paymentJournal)
    {
        _customerAccountService = customerAccountService;
        _paymentJournal = paymentJournal;

        PaymentMethods = Enum.GetValues<PaymentMethod>();
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

    public bool HasPendingPayment => _pendingPayment is not null;

    public bool CanStartPayment =>
    !IsBusy &&
    !HasPendingPayment &&
    !_paymentRecoveryBlocked;

    private void NotifyPaymentState()
    {
        OnPropertyChanged(nameof(HasPendingPayment));
        OnPropertyChanged(nameof(CanStartPayment));
    }

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
            OnPropertyChanged(nameof(CanStartPayment));
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
            OnPropertyChanged(nameof(CanStartPayment));
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

            _paymentRecoveryBlocked = false;

            _pendingPayment = await _paymentJournal.LoadAsync();
            NotifyPaymentState();

            Account =
                await _customerAccountService
                    .GetAccountAsync(customerId);

            if (Account is null)
            {
                ErrorMessage =
                    "Customer account was not found.";
            }

            if (_pendingPayment is not null)
            {
                ErrorMessage =
                    _pendingPayment.Request.CustomerId == customerId
                        ? "An unresolved customer payment exists. " +
                          "Use Retry Pending Payment before recording another payment."
                        : "An unresolved payment belongs to another customer. " +
                          "Resolve that payment before recording new payments.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _paymentRecoveryBlocked = true;
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
        if (!CanStartPayment)
        {
            ErrorMessage =
                "Resolve the pending payment before starting another.";
            return false;
        }

        if (!ValidatePayment())
            return false;

        if (!TryGetPaymentAmount(out var amount))
            return false;

        if (Account is null || amount > Account.OutstandingBalance)
        {
            ErrorMessage =
                "Payment exceeds the outstanding balance.";
            return false;
        }

        return await SubmitPaymentAsync(
            false,
            null,
            amount);
    }

    // ============================================================
    // RECEIVE PAYMENT AGAINST SELECTED INVOICE
    // ============================================================

    public async Task<bool> ReceiveInvoicePaymentAsync()
    {
        if (!CanStartPayment)
        {
            ErrorMessage =
                "Resolve the pending payment before starting another.";
            return false;
        }

        if (SelectedInvoice is null)
        {
            ErrorMessage = "Select an outstanding invoice first.";
            return false;
        }

        if (!ValidatePayment())
            return false;

        if (!TryGetPaymentAmount(out var amount))
            return false;

        if (amount > SelectedInvoice.DueAmount)
        {
            ErrorMessage =
                "Payment exceeds the selected invoice due amount.";
            return false;
        }

        return await SubmitPaymentAsync(
            true,
            SelectedInvoice.SaleId,
            amount);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    public void PrepareInvoicePayment(
        CustomerOutstandingSaleDto invoice)
    {
        if (!CanStartPayment)
        {
            return;
        }

        SelectedInvoice = invoice;

        PaymentAmount =
            (double)invoice.DueAmount;

        ErrorMessage = null;
        SuccessMessage = null;
    }

    public void PrepareAccountPayment()
    {
        if (!CanStartPayment)
        {
            return;
        }

        SelectedInvoice = null;

        PaymentAmount =
            (double)(Account?.OutstandingBalance ?? 0);

        ErrorMessage = null;
        SuccessMessage = null;
    }

    public void CancelPayment()
    {
        if (!CanStartPayment)
        {
            return;
        }

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

    private async Task<bool> SubmitPaymentAsync(
    bool isInvoicePayment,
    Guid? saleId,
    decimal amount)
    {
        if (IsBusy || HasPendingPayment || _paymentRecoveryBlocked)
        {
            ErrorMessage =
                "A payment is already processing or requires recovery.";
            return false;
        }

        var pending = new PendingCustomerPayment
        {
            IsInvoicePayment = isInvoicePayment,
            TargetSaleId = saleId,
            Request = CreatePaymentRequest(amount)
        };

        pending.Request.PaymentRequestId = Guid.NewGuid();

        try
        {
            IsSaving = true;
            ErrorMessage = null;
            SuccessMessage = null;

            await _paymentJournal.SaveAsync(pending);

            _pendingPayment = pending;
            NotifyPaymentState();

            return await ExecutePendingPaymentAsync(pending);
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

    private async Task<bool> ExecutePendingPaymentAsync(
    PendingCustomerPayment pending)
    {
        CustomerPaymentResultDto result;

        if (pending.IsInvoicePayment)
        {
            if (!pending.TargetSaleId.HasValue)
                throw new InvalidOperationException(
                    "Pending invoice payment has no target invoice.");

            result = await _customerAccountService
                .ReceiveInvoicePaymentAsync(
                    pending.TargetSaleId.Value,
                    pending.Request);
        }
        else
        {
            result = await _customerAccountService
                .ReceiveAccountPaymentAsync(
                    pending.Request);
        }

        // The backend confirmed this exact operation.
        // Clear the journal before allowing another payment.
        await _paymentJournal.ClearAsync(
            pending.Request.PaymentRequestId);

        _pendingPayment = null;
        NotifyPaymentState();

        SelectedInvoice = null;
        ClearPaymentForm();

        SuccessMessage =
            $"Payment of ৳{result.AmountApplied:N2} confirmed.";

        try
        {
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            // Payment is confirmed even if refreshing the account fails.
            ErrorMessage =
                "Payment was confirmed, but account refresh failed: " +
                ex.Message;
        }

        return true;
    }

    private bool TryGetPaymentAmount(out decimal amount)
    {
        amount = 0m;

        if (double.IsNaN(PaymentAmount) ||
            double.IsInfinity(PaymentAmount) ||
            PaymentAmount <= 0 ||
            PaymentAmount > (double)decimal.MaxValue)
        {
            ErrorMessage = "Enter a valid payment amount.";
            return false;
        }

        try
        {
            amount = Convert.ToDecimal(PaymentAmount);
            return amount > 0m;
        }
        catch (OverflowException)
        {
            ErrorMessage = "Payment amount is too large.";
            return false;
        }
    }

    public async Task<bool> RetryPendingPaymentAsync()
    {
        if (IsBusy)
            return false;

        try
        {
            IsSaving = true;
            ErrorMessage = null;

            // Always use the saved request, never a newly generated one.
            var pending = await _paymentJournal.LoadAsync();

            if (pending is null)
            {
                _pendingPayment = null;
                NotifyPaymentState();

                ErrorMessage = "No pending payment was found.";
                return false;
            }

            _pendingPayment = pending;
            NotifyPaymentState();

            if (pending.Request.CustomerId != _customerId)
            {
                ErrorMessage =
                    "This payment belongs to another customer. " +
                    "Open that customer account to recover it.";
                return false;
            }

            return await ExecutePendingPaymentAsync(pending);
        }
        catch (Exception ex)
        {
            ErrorMessage =
                "Payment outcome remains unresolved: " + ex.Message;
            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }
}