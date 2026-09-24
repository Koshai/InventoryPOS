using InventoryPOS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace InventoryPOS.Desktop.ViewModels;

public class PosPaymentLineViewModel : INotifyPropertyChanged
{
    private PaymentMethod _selectedPaymentMethod =
        PaymentMethod.Cash;

    private double _amount;

    private string _referenceNumber =
        string.Empty;

    public IReadOnlyList<PaymentMethod> PaymentMethods { get; } =
        Enum.GetValues<PaymentMethod>()
            .ToList();

    public PaymentMethod SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;

        set
        {
            if (_selectedPaymentMethod == value)
                return;

            _selectedPaymentMethod = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(RequiresReference));

            OnPropertyChanged(
                nameof(ReferenceLabel));

            if (!RequiresReference)
            {
                ReferenceNumber =
                    string.Empty;
            }
        }
    }

    public double Amount
    {
        get => _amount;

        set
        {
            if (_amount == value)
                return;

            _amount = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(DecimalAmount));

            OnPropertyChanged(
                nameof(FormattedAmount));
        }
    }

    public decimal DecimalAmount
    {
        get
        {
            if (double.IsNaN(Amount) ||
                double.IsInfinity(Amount) ||
                Amount < 0)
            {
                return 0;
            }

            return Convert.ToDecimal(
                Amount);
        }
    }

    public string FormattedAmount =>
        $"৳{DecimalAmount:N2}";

    public string ReferenceNumber
    {
        get => _referenceNumber;

        set
        {
            var normalized =
                value ?? string.Empty;

            if (_referenceNumber == normalized)
                return;

            _referenceNumber =
                normalized;

            OnPropertyChanged();
        }
    }

    public bool RequiresReference =>
        SelectedPaymentMethod !=
        PaymentMethod.Cash;

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