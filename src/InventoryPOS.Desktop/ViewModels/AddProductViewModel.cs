using InventoryPOS.Application.DTOs.Lookups;
using InventoryPOS.Application.DTOs.Products;
using InventoryPOS.Application.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class AddProductViewModel : INotifyPropertyChanged
{
    private readonly IProductService _productService;
    private readonly ICatalogLookupService _lookupService;

    private string _name = string.Empty;
    private string? _nameBn;
    private string? _description;
    private string _sku = string.Empty;

    private double _costPrice;
    private double _sellingPrice;
    private double _reorderLevel;

    private CategoryLookupDto? _selectedCategory;
    private BrandLookupDto? _selectedBrand;
    private UnitLookupDto? _selectedUnit;

    private bool _isSaving;
    private string? _errorMessage;

    public ObservableCollection<CategoryLookupDto> Categories { get; } = new();
    public ObservableCollection<BrandLookupDto> Brands { get; } = new();
    public ObservableCollection<UnitLookupDto> Units { get; } = new();

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public string? NameBn
    {
        get => _nameBn;
        set
        {
            _nameBn = value;
            OnPropertyChanged();
        }
    }

    public string? Description
    {
        get => _description;
        set
        {
            _description = value;
            OnPropertyChanged();
        }
    }

    public string SKU
    {
        get => _sku;
        set
        {
            _sku = value;
            OnPropertyChanged();
        }
    }

    public double CostPrice
    {
        get => _costPrice;
        set
        {
            _costPrice = value;
            OnPropertyChanged();
        }
    }

    public double SellingPrice
    {
        get => _sellingPrice;
        set
        {
            _sellingPrice = value;
            OnPropertyChanged();
        }
    }

    public double ReorderLevel
    {
        get => _reorderLevel;
        set
        {
            _reorderLevel = value;
            OnPropertyChanged();
        }
    }

    public CategoryLookupDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            OnPropertyChanged();
        }
    }

    public BrandLookupDto? SelectedBrand
    {
        get => _selectedBrand;
        set
        {
            _selectedBrand = value;
            OnPropertyChanged();
        }
    }

    public UnitLookupDto? SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            _selectedUnit = value;
            OnPropertyChanged();
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            _isSaving = value;
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

    public AddProductViewModel(
        IProductService productService,
        ICatalogLookupService lookupService)
    {
        _productService = productService;
        _lookupService = lookupService;
    }

    public async Task LoadAsync()
    {
        var categories = await _lookupService.GetCategoriesAsync();
        var brands = await _lookupService.GetBrandsAsync();
        var units = await _lookupService.GetUnitsAsync();

        Categories.Clear();
        Brands.Clear();
        Units.Clear();

        foreach (var item in categories)
            Categories.Add(item);

        foreach (var item in brands)
            Brands.Add(item);

        foreach (var item in units)
            Units.Add(item);

        SelectedCategory = Categories.FirstOrDefault();
        SelectedUnit = Units.FirstOrDefault();
    }

    public async Task<Guid?> SaveAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Product name is required.";
            return null;
        }

        if (string.IsNullOrWhiteSpace(SKU))
        {
            ErrorMessage = "SKU is required.";
            return null;
        }

        if (SelectedCategory is null)
        {
            ErrorMessage = "Category is required.";
            return null;
        }

        if (SelectedUnit is null)
        {
            ErrorMessage = "Unit is required.";
            return null;
        }

        try
        {
            IsSaving = true;

            return await _productService.CreateAsync(
                new CreateProductRequest
                {
                    Name = Name,
                    NameBn = NameBn,
                    Description = Description,
                    CategoryId = SelectedCategory.Id,
                    BrandId = SelectedBrand?.Id,
                    UnitId = SelectedUnit.Id,
                    SKU = SKU,
                    CostPrice = Convert.ToDecimal(CostPrice),
                    SellingPrice = Convert.ToDecimal(SellingPrice),
                    ReorderLevel = Convert.ToDecimal(ReorderLevel),
                    IsActive = true
                });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            IsSaving = false;
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