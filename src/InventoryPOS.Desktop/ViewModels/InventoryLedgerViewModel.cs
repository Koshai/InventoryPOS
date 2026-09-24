using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class InventoryLedgerViewModel : INotifyPropertyChanged
{
    private readonly IInventoryService _inventoryService;

    private string _searchText = string.Empty;
    private bool _isLoading;
    private string? _errorMessage;

    public ObservableCollection<InventoryLedgerItemDto>
        LedgerItems
    { get; }
        = new();

    public ObservableCollection<InventoryLedgerItemDto>
        FilteredItems
    { get; }
        = new();

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

    public InventoryLedgerViewModel(
        IInventoryService inventoryService)
    {
        _inventoryService =
            inventoryService;
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            LedgerItems.Clear();

            var items =
                await _inventoryService
                    .GetInventoryLedgerAsync();

            foreach (var item in items)
            {
                LedgerItems.Add(item);
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();

        var query =
            SearchText?.Trim();

        var items =
            string.IsNullOrWhiteSpace(query)
                ? LedgerItems
                : new ObservableCollection<InventoryLedgerItemDto>(
                    LedgerItems.Where(x =>
                        x.ProductName.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ||

                        x.SKU.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ||

                        x.LocationName.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ||

                        x.MovementType.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ||

                        (x.ReferenceType?.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase)
                            ?? false) ||

                        (x.Notes?.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase)
                            ?? false)));

        foreach (var item in items)
        {
            FilteredItems.Add(item);
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