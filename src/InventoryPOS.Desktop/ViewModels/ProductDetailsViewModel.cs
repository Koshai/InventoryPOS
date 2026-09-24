using InventoryPOS.Application.DTOs.Products;
using InventoryPOS.Application.Interfaces;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class ProductDetailsViewModel : INotifyPropertyChanged
{
    private readonly IProductService _productService;

    private ProductDetailsDto? _product;
    private bool _isLoading;
    private string? _errorMessage;

    public ProductDetailsDto? Product
    {
        get => _product;
        private set
        {
            _product = value;
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

    public ProductDetailsViewModel(
        IProductService productService)
    {
        _productService = productService;
    }

    public async Task LoadAsync(Guid productId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            Product =
                await _productService.GetByIdAsync(productId);

            if (Product is null)
            {
                ErrorMessage = "Product could not be found.";
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