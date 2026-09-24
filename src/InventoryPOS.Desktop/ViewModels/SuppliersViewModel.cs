using InventoryPOS.Application.DTOs.Suppliers;
using InventoryPOS.Application.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class SuppliersViewModel : INotifyPropertyChanged
{
    private readonly ISupplierService _supplierService;

    private string _searchText = string.Empty;
    private bool _isLoading;
    private string? _errorMessage;

    public ObservableCollection<SupplierListItemDto>
        Suppliers
    { get; } = new();

    public ObservableCollection<SupplierListItemDto>
        FilteredSuppliers
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
        }
    }

    public SuppliersViewModel(
        ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            Suppliers.Clear();

            var suppliers =
                await _supplierService.GetAllAsync();

            foreach (var supplier in suppliers)
            {
                Suppliers.Add(supplier);
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
        FilteredSuppliers.Clear();

        var query =
            SearchText?.Trim();

        var items =
            string.IsNullOrWhiteSpace(query)
                ? Suppliers
                : new ObservableCollection<SupplierListItemDto>(
                    Suppliers.Where(x =>
                        x.SupplierCode.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ||
                        x.Name.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ||
                        (x.ContactPerson?.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (x.Phone?.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ?? false)));

        foreach (var item in items)
        {
            FilteredSuppliers.Add(item);
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