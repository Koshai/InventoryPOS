using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class PurchaseReturnDetailsViewModel : INotifyPropertyChanged
{
    private readonly IPurchaseReturnService _purchaseReturnService;

    private PurchaseReturnDetailsDto? _return;
    private bool _isBusy;
    private string? _errorMessage;

    public PurchaseReturnDetailsViewModel(
        IPurchaseReturnService purchaseReturnService)
    {
        _purchaseReturnService = purchaseReturnService;
    }

    public PurchaseReturnDetailsDto? Return
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
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(ReturnDateDisplay));
            OnPropertyChanged(nameof(ReceiptNumber));
            OnPropertyChanged(nameof(PurchaseOrderNumber));
            OnPropertyChanged(nameof(SupplierName));
            OnPropertyChanged(nameof(LocationName));
            OnPropertyChanged(nameof(TotalQuantityDisplay));
            OnPropertyChanged(nameof(TotalAmountDisplay));
            OnPropertyChanged(nameof(ReasonDisplay));
            OnPropertyChanged(nameof(NotesDisplay));
            OnPropertyChanged(nameof(Lines));
        }
    }

    public bool HasReturn => Return is not null;

    public string ReturnNumber =>
        Return?.ReturnNumber ?? string.Empty;

    public string Status =>
        Return?.Status.ToString() ?? string.Empty;

    public string ReturnDateDisplay =>
        Return is null
            ? string.Empty
            : Return.ReturnDate.ToLocalTime()
                .ToString("dd MMM yyyy, h:mm tt");

    public string ReceiptNumber =>
        Return?.ReceiptNumber ?? string.Empty;

    public string PurchaseOrderNumber =>
        Return?.PurchaseOrderNumber ?? string.Empty;

    public string SupplierName =>
        Return?.SupplierName ?? string.Empty;

    public string LocationName =>
        Return?.LocationName ?? string.Empty;

    public string TotalQuantityDisplay =>
        Return is null
            ? "0"
            : Return.TotalQuantity.ToString("0.###");

    public string TotalAmountDisplay =>
        Return is null
            ? "৳0.00"
            : $"৳{Return.TotalAmount:N2}";

    public string ReasonDisplay =>
        string.IsNullOrWhiteSpace(Return?.Reason)
            ? "-"
            : Return!.Reason!;

    public string NotesDisplay =>
        string.IsNullOrWhiteSpace(Return?.Notes)
            ? "-"
            : Return!.Notes!;

    public IReadOnlyList<PurchaseReturnDetailsLineDto> Lines => Return?.Lines ?? new List<PurchaseReturnDetailsLineDto>();

    public bool IsBusy
    {
        get => _isBusy;

        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
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

    public async Task LoadAsync(Guid purchaseReturnId)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Return =
                await _purchaseReturnService
                    .GetByIdAsync(purchaseReturnId);

            if (Return is null)
            {
                ErrorMessage =
                    "Purchase return could not be found.";
            }
        }
        catch (Exception ex)
        {
            Return = null;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}