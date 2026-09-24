using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class PurchaseOrdersViewModel : INotifyPropertyChanged
{
    private readonly IPurchaseOrderService _purchaseOrderService;

    private string _searchText = string.Empty;
    private bool _isLoading;
    private string? _errorMessage;

    public ObservableCollection<PurchaseOrderListItemDto>
        PurchaseOrders
    { get; } = new();

    public ObservableCollection<PurchaseOrderListItemDto>
        FilteredPurchaseOrders
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

    public PurchaseOrdersViewModel(
        IPurchaseOrderService purchaseOrderService)
    {
        _purchaseOrderService = purchaseOrderService;
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            PurchaseOrders.Clear();

            var orders =
                await _purchaseOrderService.GetAllAsync();

            foreach (var order in orders)
            {
                PurchaseOrders.Add(order);
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
        FilteredPurchaseOrders.Clear();

        var query =
            SearchText?.Trim();

        var results =
            string.IsNullOrWhiteSpace(query)
                ? PurchaseOrders
                : new ObservableCollection<PurchaseOrderListItemDto>(
                    PurchaseOrders.Where(x =>
                        x.OrderNumber.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ||
                        x.SupplierName.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ||
                        x.LocationName.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ||
                        x.Status.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase)));

        foreach (var item in results)
        {
            FilteredPurchaseOrders.Add(item);
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
            new PropertyChangedEventArgs(propertyName));
    }
}