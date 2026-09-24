using System;
using System.Linq;
using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class InventoryViewModel : INotifyPropertyChanged
{
    private readonly IInventoryService _inventoryService;

    private bool _isLoading;
    private string _searchText = string.Empty;

    public ObservableCollection<InventoryItemDto> InventoryItems { get; }
        = new();

    public ObservableCollection<InventoryItemDto> FilteredItems { get; }
        = new();

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

    public InventoryViewModel(
        IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;

            var items =
                await _inventoryService.GetInventoryAsync();

            InventoryItems.Clear();

            foreach (var item in items)
            {
                InventoryItems.Add(item);
            }

            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();

        var query = SearchText.Trim();

        IEnumerable<InventoryItemDto> items =
            InventoryItems;

        if (!string.IsNullOrWhiteSpace(query))
        {
            items = items.Where(x =>
                x.ProductName.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase)
                ||
                x.SKU.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase)
                ||
                x.LocationName.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in items)
        {
            FilteredItems.Add(item);
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