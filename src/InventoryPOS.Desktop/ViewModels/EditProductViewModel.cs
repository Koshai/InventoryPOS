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

public class EditProductViewModel : INotifyPropertyChanged
{
    private readonly IProductService _productService;
    private readonly ICatalogLookupService _lookupService;

    private Guid _productId;
    private Guid? _variantId;

    // ---------------------------------------------------------
    // Product fields
    // ---------------------------------------------------------

    private string _name = string.Empty;
    private string? _nameBn;
    private string? _description;

    private CategoryLookupDto? _selectedCategory;
    private BrandLookupDto? _selectedBrand;
    private UnitLookupDto? _selectedUnit;

    private bool _isActive = true;

    // ---------------------------------------------------------
    // Primary ProductVariant fields
    //
    // NumberBox uses double, so prices/reorder level are exposed
    // as double in the ViewModel and converted back to decimal
    // when saving.
    // ---------------------------------------------------------

    private string _sku = string.Empty;
    private double _costPrice;
    private double _sellingPrice;
    private double _reorderLevel;
    private double _defaultWarrantyMonths;
    private bool _variantIsActive = true;

    // ---------------------------------------------------------
    // UI state
    // ---------------------------------------------------------

    private bool _isLoading;
    private bool _isSaving;
    private string? _errorMessage;

    // ---------------------------------------------------------
    // Lookup collections
    // ---------------------------------------------------------

    public ObservableCollection<CategoryLookupDto> Categories { get; }
        = new();

    public ObservableCollection<BrandLookupDto> Brands { get; }
        = new();

    public ObservableCollection<UnitLookupDto> Units { get; }
        = new();

    // ---------------------------------------------------------
    // Product properties
    // ---------------------------------------------------------

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
                return;

            _name = value;
            OnPropertyChanged();
        }
    }

    public string? NameBn
    {
        get => _nameBn;
        set
        {
            if (_nameBn == value)
                return;

            _nameBn = value;
            OnPropertyChanged();
        }
    }

    public string? Description
    {
        get => _description;
        set
        {
            if (_description == value)
                return;

            _description = value;
            OnPropertyChanged();
        }
    }

    public CategoryLookupDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory == value)
                return;

            _selectedCategory = value;
            OnPropertyChanged();
        }
    }

    public BrandLookupDto? SelectedBrand
    {
        get => _selectedBrand;
        set
        {
            if (_selectedBrand == value)
                return;

            _selectedBrand = value;
            OnPropertyChanged();
        }
    }

    public UnitLookupDto? SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            if (_selectedUnit == value)
                return;

            _selectedUnit = value;
            OnPropertyChanged();
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value)
                return;

            _isActive = value;
            OnPropertyChanged();
        }
    }

    // ---------------------------------------------------------
    // Primary ProductVariant properties
    // ---------------------------------------------------------

    public string SKU
    {
        get => _sku;
        set
        {
            if (_sku == value)
                return;

            _sku = value;
            OnPropertyChanged();
        }
    }

    public double CostPrice
    {
        get => _costPrice;
        set
        {
            if (_costPrice == value)
                return;

            _costPrice = value;
            OnPropertyChanged();
        }
    }

    public double SellingPrice
    {
        get => _sellingPrice;
        set
        {
            if (_sellingPrice == value)
                return;

            _sellingPrice = value;
            OnPropertyChanged();
        }
    }

    public double ReorderLevel
    {
        get => _reorderLevel;
        set
        {
            if (_reorderLevel == value)
                return;

            _reorderLevel = value;
            OnPropertyChanged();
        }
    }

    public double DefaultWarrantyMonths
    {
        get => _defaultWarrantyMonths;
        set
        {
            if (_defaultWarrantyMonths == value)
                return;

            _defaultWarrantyMonths = value;
            OnPropertyChanged();
        }
    }

    public bool VariantIsActive
    {
        get => _variantIsActive;
        set
        {
            if (_variantIsActive == value)
                return;

            _variantIsActive = value;
            OnPropertyChanged();
        }
    }

    // ---------------------------------------------------------
    // UI state properties
    // ---------------------------------------------------------

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

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (_isSaving == value)
                return;

            _isSaving = value;
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

    // ---------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------

    public EditProductViewModel(
        IProductService productService,
        ICatalogLookupService lookupService)
    {
        _productService = productService;
        _lookupService = lookupService;
    }

    // ---------------------------------------------------------
    // Load
    // ---------------------------------------------------------

    public async Task LoadAsync(Guid productId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            _productId = productId;

            // Load lookup data sequentially.
            // This avoids concurrent operations if the services
            // share the same scoped DbContext.

            var categories =
                await _lookupService.GetCategoriesAsync();

            var brands =
                await _lookupService.GetBrandsAsync();

            var units =
                await _lookupService.GetUnitsAsync();

            var product =
                await _productService.GetByIdAsync(
                    productId);

            // -------------------------------------------------
            // Populate lookup collections
            // -------------------------------------------------

            Categories.Clear();

            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            Brands.Clear();

            foreach (var brand in brands)
            {
                Brands.Add(brand);
            }

            Units.Clear();

            foreach (var unit in units)
            {
                Units.Add(unit);
            }

            // -------------------------------------------------
            // Validate product
            // -------------------------------------------------

            if (product is null)
            {
                ErrorMessage =
                    "Product could not be found.";

                return;
            }

            // -------------------------------------------------
            // Load Product
            // -------------------------------------------------

            Name =
                product.Name;

            NameBn =
                product.NameBn;

            Description =
                product.Description;

            IsActive =
                product.IsActive;

            SelectedCategory =
                Categories.FirstOrDefault(
                    x => x.Id == product.CategoryId);

            SelectedBrand =
                product.BrandId.HasValue
                    ? Brands.FirstOrDefault(
                        x =>
                            x.Id ==
                            product.BrandId.Value)
                    : null;

            SelectedUnit =
                Units.FirstOrDefault(
                    x => x.Id == product.UnitId);

            // -------------------------------------------------
            // Load primary/default ProductVariant
            //
            // For the current MVP we edit the first variant.
            // Later this can become a dedicated multi-variant UI.
            // -------------------------------------------------

            var variant =
                product.Variants
                    .OrderBy(x => x.Id)
                    .FirstOrDefault();

            if (variant is null)
            {
                ErrorMessage =
                    "This product does not have a product variant.";

                return;
            }

            _variantId =
                variant.Id;

            SKU =
                variant.SKU;

            CostPrice =
                Convert.ToDouble(
                    variant.CostPrice);

            SellingPrice =
                Convert.ToDouble(
                    variant.SellingPrice);

            ReorderLevel =
                Convert.ToDouble(
                    variant.ReorderLevel);

            DefaultWarrantyMonths =
                    variant.DefaultWarrantyMonths ?? 0;

            VariantIsActive =
                variant.IsActive;
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

    // ---------------------------------------------------------
    // Save
    // ---------------------------------------------------------

    public async Task<bool> SaveAsync()
    {
        ErrorMessage = null;

        // -----------------------------------------------------
        // Product validation
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage =
                "Product name is required.";

            return false;
        }

        if (SelectedCategory is null)
        {
            ErrorMessage =
                "Category is required.";

            return false;
        }

        if (SelectedUnit is null)
        {
            ErrorMessage =
                "Unit is required.";

            return false;
        }

        // -----------------------------------------------------
        // Variant validation
        // -----------------------------------------------------

        if (!_variantId.HasValue)
        {
            ErrorMessage =
                "Product variant could not be found.";

            return false;
        }

        if (string.IsNullOrWhiteSpace(SKU))
        {
            ErrorMessage =
                "SKU is required.";

            return false;
        }

        if (CostPrice < 0)
        {
            ErrorMessage =
                "Cost price cannot be negative.";

            return false;
        }

        if (SellingPrice <= 0)
        {
            ErrorMessage =
                "Selling price must be greater than zero.";

            return false;
        }

        if (ReorderLevel < 0)
        {
            ErrorMessage =
                "Reorder level cannot be negative.";

            return false;
        }

        if (double.IsNaN(DefaultWarrantyMonths) ||
            double.IsInfinity(DefaultWarrantyMonths) ||
            DefaultWarrantyMonths < 0)
        {
            ErrorMessage =
                "Please enter a valid warranty period.";

            return false;
        }

        if (DefaultWarrantyMonths % 1 != 0)
        {
            ErrorMessage =
                "Warranty must be entered as whole months.";

            return false;
        }

        // NumberBox can technically produce NaN.
        // Prevent invalid values from reaching decimal conversion.

        if (double.IsNaN(CostPrice) ||
            double.IsInfinity(CostPrice))
        {
            ErrorMessage =
                "Please enter a valid cost price.";

            return false;
        }

        if (double.IsNaN(SellingPrice) ||
            double.IsInfinity(SellingPrice))
        {
            ErrorMessage =
                "Please enter a valid selling price.";

            return false;
        }

        if (double.IsNaN(ReorderLevel) ||
            double.IsInfinity(ReorderLevel))
        {
            ErrorMessage =
                "Please enter a valid reorder level.";

            return false;
        }

        // -----------------------------------------------------
        // Save
        // -----------------------------------------------------

        try
        {
            IsSaving = true;

            await _productService.UpdateAsync(
                new UpdateProductRequest
                {
                    // Product

                    Id =
                        _productId,

                    Name =
                        Name,

                    NameBn =
                        NameBn,

                    Description =
                        Description,

                    CategoryId =
                        SelectedCategory.Id,

                    BrandId =
                        SelectedBrand?.Id,

                    UnitId =
                        SelectedUnit.Id,

                    IsActive =
                        IsActive,

                    // Primary ProductVariant

                    VariantId =
                        _variantId,

                    SKU =
                        SKU,

                    CostPrice =
                        Convert.ToDecimal(
                            CostPrice),

                    SellingPrice =
                        Convert.ToDecimal(
                            SellingPrice),

                    ReorderLevel =
                        Convert.ToDecimal(
                            ReorderLevel),

                    DefaultWarrantyMonths =
                        DefaultWarrantyMonths <= 0
                            ? null
                            : Convert.ToInt32(
                                DefaultWarrantyMonths),

                    VariantIsActive =
                        VariantIsActive
                });

            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;

            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    // ---------------------------------------------------------
    // INotifyPropertyChanged
    // ---------------------------------------------------------

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
}