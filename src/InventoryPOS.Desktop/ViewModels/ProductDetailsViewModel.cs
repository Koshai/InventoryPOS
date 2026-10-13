using InventoryPOS.Application.DTOs.Products;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class ProductDetailsViewModel : INotifyPropertyChanged
{
    private readonly IProductService _productService;
    private readonly IProductBarcodeService _productBarcodeService;

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

    public ObservableCollection<ProductBarcodeDto>
    Barcodes
    { get; } = new();

    private Guid? _selectedVariantId;

    public Guid? SelectedVariantId
    {
        get => _selectedVariantId;
        private set
        {
            _selectedVariantId = value;
            OnPropertyChanged();
        }
    }

    private string? _selectedVariantSku;

    public string? SelectedVariantSku
    {
        get => _selectedVariantSku;
        private set
        {
            _selectedVariantSku = value;
            OnPropertyChanged();
        }
    }

    public ProductDetailsViewModel(
    IProductService productService,
    IProductBarcodeService productBarcodeService)
    {
        _productService =
            productService;

        _productBarcodeService =
            productBarcodeService;
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

    public async Task LoadBarcodesAsync(
    Guid variantId,
    string sku)
    {
        try
        {
            ErrorMessage = null;

            SelectedVariantId =
                variantId;

            SelectedVariantSku =
                sku;

            Barcodes.Clear();

            var barcodes =
                await _productBarcodeService
                    .GetForVariantAsync(
                        variantId);

            foreach (var barcode in barcodes)
            {
                Barcodes.Add(barcode);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public async Task AddBarcodeAsync(
    string value,
    BarcodeType type,
    bool isPrimary)
    {
        if (!SelectedVariantId.HasValue)
        {
            throw new InvalidOperationException(
                "No product variant is selected.");
        }

        await _productBarcodeService.AddAsync(
            SelectedVariantId.Value,
            value,
            type,
            isPrimary);

        await ReloadBarcodesAsync();
    }

    public async Task DeleteBarcodeAsync(
    Guid barcodeId)
    {
        await _productBarcodeService
            .DeleteAsync(barcodeId);

        await ReloadBarcodesAsync();
    }

    private async Task ReloadBarcodesAsync()
    {
        if (!SelectedVariantId.HasValue)
            return;

        Barcodes.Clear();

        var barcodes =
            await _productBarcodeService
                .GetForVariantAsync(
                    SelectedVariantId.Value);

        foreach (var barcode in barcodes)
        {
            Barcodes.Add(barcode);
        }
    }
}