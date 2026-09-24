using InventoryPOS.Application.DTOs.Suppliers;
using InventoryPOS.Application.Interfaces;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class SupplierDetailsViewModel : INotifyPropertyChanged
{
    private readonly ISupplierService _supplierService;

    private SupplierDetailsDto? _supplier;
    private bool _isLoading;
    private string? _errorMessage;

    public SupplierDetailsDto? Supplier
    {
        get => _supplier;
        private set
        {
            if (_supplier == value) return;
            _supplier = value;
            OnPropertyChanged();
        }
    }

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
        }
    }

    public SupplierDetailsViewModel(
        ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    public async Task LoadAsync(Guid supplierId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            Supplier =
                await _supplierService.GetByIdAsync(
                    supplierId);

            if (Supplier is null)
            {
                ErrorMessage =
                    "Supplier could not be found.";
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