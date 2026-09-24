using InventoryPOS.Application.DTOs.Customers;
using InventoryPOS.Application.Interfaces;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class CustomerDetailsViewModel : INotifyPropertyChanged
{
    private readonly ICustomerService _customerService;

    private CustomerDetailsDto? _customer;
    private bool _isLoading;
    private string? _errorMessage;

    public CustomerDetailsDto? Customer
    {
        get => _customer;
        private set
        {
            _customer = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public CustomerDetailsViewModel(
        ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task LoadAsync(Guid customerId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            Customer =
                await _customerService
                    .GetByIdAsync(customerId);

            if (Customer is null)
            {
                ErrorMessage =
                    "Customer could not be found.";
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}