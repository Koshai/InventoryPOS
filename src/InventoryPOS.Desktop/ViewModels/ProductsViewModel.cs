using InventoryPOS.Application.DTOs.Products;
using InventoryPOS.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Linq;

namespace InventoryPOS.Desktop.ViewModels;

public class ProductsViewModel : INotifyPropertyChanged
{
    private readonly IProductService _productService;

    private bool _isLoading;
    private string _searchText = string.Empty;

    public ObservableCollection<ProductListItemDto> Products { get; }
        = new();

    public ObservableCollection<ProductListItemDto> FilteredProducts { get; }
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

    public ProductsViewModel(
        IProductService productService)
    {
        _productService = productService;
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;

            var products =
                await _productService.GetAllAsync();

            Products.Clear();

            foreach (var product in products)
            {
                Products.Add(product);
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
        FilteredProducts.Clear();

        var query = SearchText.Trim();

        IEnumerable<ProductListItemDto> products =
            Products;

        if (!string.IsNullOrWhiteSpace(query))
        {
            products = products.Where(x =>
                x.Name.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase)
                ||
                (x.NameBn?.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase) ?? false)
                ||
                x.SKU.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase)
                ||
                x.CategoryName.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase)
                ||
                (x.BrandName?.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase) ?? false));
        }

        foreach (var product in products)
        {
            FilteredProducts.Add(product);
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