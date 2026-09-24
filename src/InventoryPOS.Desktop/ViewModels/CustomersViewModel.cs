using InventoryPOS.Application.DTOs.Customers;
using InventoryPOS.Application.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class CustomersViewModel : INotifyPropertyChanged
{
    private readonly ICustomerService _customerService;

    private string _searchText = string.Empty;
    private bool _isLoading;
    private string? _errorMessage;

    public ObservableCollection<CustomerListItemDto>
        Customers
    { get; } = new();

    public ObservableCollection<CustomerListItemDto>
        FilteredCustomers
    { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
                return;

            _searchText = value;

            OnPropertyChanged();

            ApplyFilter();
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

    public CustomersViewModel(
        ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var customers =
                await _customerService.GetAllAsync();

            Customers.Clear();

            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }

            ApplyFilter();
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

    private void ApplyFilter()
    {
        FilteredCustomers.Clear();

        var query =
            SearchText?.Trim();

        var result =
            string.IsNullOrWhiteSpace(query)
                ? Customers
                : Customers.Where(x =>
                    x.CustomerCode.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase) ||

                    x.Name.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase) ||

                    (x.Phone?.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase)
                        ?? false) ||

                    (x.Email?.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase)
                        ?? false));

        foreach (var customer in result)
        {
            FilteredCustomers.Add(customer);
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