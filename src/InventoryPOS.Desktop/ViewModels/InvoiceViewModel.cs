using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Interfaces;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class InvoiceViewModel : INotifyPropertyChanged
{
    private readonly ISaleService _saleService;

    private SaleDetailsDto? _sale;
    private bool _isLoading;
    private string? _errorMessage;

    public InvoiceViewModel(ISaleService saleService)
    {
        _saleService = saleService;
    }

    public SaleDetailsDto? Sale
    {
        get => _sale;
        private set
        {
            if (_sale == value)
                return;

            _sale = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSale));
            OnPropertyChanged(nameof(HasCustomerCode));
            OnPropertyChanged(nameof(HasCustomerPhone));
            OnPropertyChanged(nameof(HasCustomerAddress));
            OnPropertyChanged(nameof(HasNotes));
        }
    }

    public bool HasSale =>
        Sale is not null;

    public bool HasCustomerCode =>
        !string.IsNullOrWhiteSpace(Sale?.CustomerCode);

    public bool HasCustomerPhone =>
        !string.IsNullOrWhiteSpace(Sale?.CustomerPhone);

    public bool HasCustomerAddress =>
        !string.IsNullOrWhiteSpace(Sale?.CustomerAddress);

    public bool HasNotes =>
        !string.IsNullOrWhiteSpace(Sale?.Notes);

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

    public async Task LoadAsync(Guid saleId)
    {
        if (saleId == Guid.Empty)
        {
            SetError("A valid sale was not supplied.");
            return;
        }

        try
        {
            IsLoading = true;
            SetError(null);

            Sale =
                await _saleService.GetSaleDetailsAsync(
                    saleId);

            if (Sale is null)
            {
                SetError(
                    "The requested sale could not be found.");
            }
        }
        catch (Exception ex)
        {
            Sale = null;
            SetError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetError(string? message)
    {
        ErrorMessage = message;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}