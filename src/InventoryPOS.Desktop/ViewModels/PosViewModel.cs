using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class PosViewModel : INotifyPropertyChanged
{
    private readonly ISaleService _saleService;
    private readonly ISalesQuoteService _salesQuoteService;

    private LocationLookupDto? _selectedLocation;
    private PosCustomerDto? _selectedCustomer;

    private string _searchText =
        string.Empty;

    private bool _isLoading;
    private bool _isSaving;

    private string? _errorMessage;
    private string? _lastInvoiceNumber;
    private Task _productLoadTask = Task.CompletedTask;
    private Guid? _loadedQuoteId;
    private string? _loadedQuoteNumber;

    public Guid? LoadedQuoteId => _loadedQuoteId;
    public string? LoadedQuoteNumber => _loadedQuoteNumber;
    public bool HasLoadedQuote => _loadedQuoteId.HasValue;

    // ---------------------------------------------------------
    // Collections
    // ---------------------------------------------------------

    public ObservableCollection<LocationLookupDto>
        Locations
    { get; } = new();

    public ObservableCollection<PosProductDto>
        Products
    { get; } = new();

    public ObservableCollection<PosProductDto>
        FilteredProducts
    { get; } = new();

    public ObservableCollection<PosCustomerDto>
        Customers
    { get; } = new();

    public ObservableCollection<PosCartLineViewModel>
        CartLines
    { get; } = new();

    public ObservableCollection<PosPaymentLineViewModel>
        PaymentLines
    { get; } = new();

    // ---------------------------------------------------------
    // Location
    // ---------------------------------------------------------

    public LocationLookupDto? SelectedLocation
    {
        get => _selectedLocation;

        set
        {
            if (_selectedLocation == value)
                return;

            _selectedLocation = value;

            OnPropertyChanged();

            _productLoadTask = LoadProductsAsync();
        }
    }

    // ---------------------------------------------------------
    // Customer
    // ---------------------------------------------------------

    public PosCustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;

        set
        {
            if (_selectedCustomer == value)
                return;

            _selectedCustomer = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(HasSelectedCustomer));

            OnPropertyChanged(
                nameof(CustomerDisplay));

            OnPropertyChanged(
                nameof(FormattedCustomerCreditLimit));

            OnPropertyChanged(
                nameof(FormattedCustomerOutstanding));

            OnPropertyChanged(
                nameof(FormattedCustomerAvailableCredit));

            OnPropertyChanged(
                nameof(CanUseCredit));

            RefreshPaymentSummary();
        }
    }

    public bool HasSelectedCustomer =>
        SelectedCustomer is not null;

    public string CustomerDisplay =>
        SelectedCustomer is null
            ? "Walk-in Customer"
            : SelectedCustomer.DisplayName;

    public string FormattedCustomerCreditLimit =>
        SelectedCustomer is null
            ? "৳0.00"
            : $"৳{SelectedCustomer.CreditLimit:N2}";

    public string FormattedCustomerOutstanding =>
        SelectedCustomer is null
            ? "৳0.00"
            : $"৳{SelectedCustomer.OutstandingBalance:N2}";

    public string FormattedCustomerAvailableCredit =>
        SelectedCustomer is null
            ? "৳0.00"
            : $"৳{SelectedCustomer.AvailableCredit:N2}";

    // ---------------------------------------------------------
    // Search
    // ---------------------------------------------------------

    public string SearchText
    {
        get => _searchText;

        set
        {
            if (_searchText == value)
                return;

            _searchText = value;

            OnPropertyChanged();

            ApplyProductFilter();
        }
    }

    // ---------------------------------------------------------
    // Payment
    // ---------------------------------------------------------

    public decimal PaidAmount =>
        PaymentLines.Sum(
            x => x.DecimalAmount);

    public decimal DueAmount =>
        Math.Max(
            0,
            Total - PaidAmount);

    public bool HasDue =>
        DueAmount > 0;

    public bool HasPayments =>
        PaymentLines.Count > 0;

    public bool CanUseCredit =>
        SelectedCustomer is not null;

    public string FormattedPaidAmount =>
        $"৳{PaidAmount:N2}";

    public string FormattedDueAmount =>
        $"৳{DueAmount:N2}";

    public string PaymentStatusDisplay
    {
        get
        {
            if (Total <= 0)
                return "No sale";

            if (PaidAmount >= Total)
                return "Fully Paid";

            if (PaidAmount <= 0)
            {
                return SelectedCustomer is null
                    ? "Payment Required"
                    : "Credit";
            }

            return "Partially Paid";
        }
    }

    // ---------------------------------------------------------
    // UI State
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

    public string? LastInvoiceNumber
    {
        get => _lastInvoiceNumber;

        private set
        {
            if (_lastInvoiceNumber == value)
                return;

            _lastInvoiceNumber = value;

            OnPropertyChanged();
        }
    }

    // ---------------------------------------------------------
    // Totals
    // ---------------------------------------------------------

    public decimal Subtotal =>
        CartLines.Sum(
            x => x.LineTotal);

    public decimal Total =>
        Subtotal;

    public string FormattedSubtotal =>
        $"৳{Subtotal:N2}";

    public string FormattedTotal =>
        $"৳{Total:N2}";

    public bool HasItems =>
        CartLines.Count > 0;

    // ---------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------

    public PosViewModel(
    ISaleService saleService,
    ISalesQuoteService salesQuoteService)
    {
        _saleService =
            saleService;

        _salesQuoteService =
            salesQuoteService;
    }

    // ---------------------------------------------------------
    // Initial Load
    // ---------------------------------------------------------

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var locations =
                await _saleService
                    .GetLocationsAsync();

            Locations.Clear();

            foreach (var location in locations)
            {
                Locations.Add(
                    location);
            }

            var customers =
                await _saleService
                    .GetCustomersAsync();

            Customers.Clear();

            foreach (var customer in customers)
            {
                Customers.Add(
                    customer);
            }

            SelectedCustomer = null;

            if (Locations.Count > 0)
            {
                SelectedLocation =
                    Locations[0];
                await _productLoadTask;
            }
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
    // Products
    // ---------------------------------------------------------

    private async Task LoadProductsAsync()
    {
        if (SelectedLocation is null)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var products =
                await _saleService
                    .GetProductsAsync(
                        SelectedLocation.Id);

            Products.Clear();

            foreach (var product in products)
            {
                Products.Add(
                    product);
            }

            ApplyProductFilter();
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

    private void ApplyProductFilter()
    {
        FilteredProducts.Clear();

        IEnumerable<PosProductDto> result =
            Products;

        var query =
            SearchText?.Trim();

        if (!string.IsNullOrWhiteSpace(
                query))
        {
            result =
                result.Where(
                    x =>
                        x.SKU.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ||

                        x.ProductName.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase));
        }

        foreach (var product in result)
        {
            FilteredProducts.Add(
                product);
        }
    }

    // ---------------------------------------------------------
    // Cart
    // ---------------------------------------------------------

    public void AddProduct(
        PosProductDto product)
    {
        ErrorMessage = null;

        if (product.QuantityAvailable <= 0)
        {
            ErrorMessage =
                "This product is out of stock.";

            return;
        }

        var previousTotal =
            Total;

        var existing =
            CartLines.FirstOrDefault(
                x =>
                    x.ProductVariantId ==
                    product.ProductVariantId);

        if (existing is not null)
        {
            if (Convert.ToDecimal(
                    existing.Quantity + 1) >
                existing.QuantityAvailable)
            {
                ErrorMessage =
                    $"Only {existing.QuantityAvailable:N3} " +
                    $"{existing.UnitSymbol} available.";

                return;
            }

            existing.Quantity += 1;

            RefreshTotals();

            UpdatePaymentsAfterCartChange(
                previousTotal);

            return;
        }

        var line =
            new PosCartLineViewModel(
                product);

        line.PropertyChanged +=
            CartLine_PropertyChanged;

        CartLines.Add(
            line);

        RefreshTotals();

        UpdatePaymentsAfterCartChange(
            previousTotal);
    }

    public void RemoveLine(
        PosCartLineViewModel line)
    {
        var previousTotal =
            Total;

        line.PropertyChanged -=
            CartLine_PropertyChanged;

        CartLines.Remove(
            line);

        RefreshTotals();

        UpdatePaymentsAfterCartChange(
            previousTotal);
    }

    public void ValidateQuantity(
        PosCartLineViewModel line)
    {
        if (line.Quantity <= 0)
        {
            line.Quantity = 1;
        }

        if (Convert.ToDecimal(
                line.Quantity) >
            line.QuantityAvailable)
        {
            line.Quantity =
                Convert.ToDouble(
                    line.QuantityAvailable);

            ErrorMessage =
                $"Only {line.QuantityAvailable:N3} " +
                $"{line.UnitSymbol} available.";
        }

        RefreshTotals();
    }

    private void CartLine_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
            nameof(
                PosCartLineViewModel.Quantity))
        {
            RefreshTotals();
        }
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(
            nameof(Subtotal));

        OnPropertyChanged(
            nameof(Total));

        OnPropertyChanged(
            nameof(FormattedSubtotal));

        OnPropertyChanged(
            nameof(FormattedTotal));

        OnPropertyChanged(
            nameof(HasItems));

        RefreshPaymentSummary();
    }

    // ---------------------------------------------------------
    // Payment Lines
    // ---------------------------------------------------------

    public void AddPayment()
    {
        ErrorMessage = null;

        var remaining =
            Math.Max(
                0,
                Total - PaidAmount);

        if (remaining <= 0)
        {
            ErrorMessage =
                "The sale is already fully paid.";

            return;
        }

        var payment =
            new PosPaymentLineViewModel
            {
                SelectedPaymentMethod =
                    PaymentMethod.Cash,

                Amount =
                    Convert.ToDouble(
                        remaining)
            };

        payment.PropertyChanged +=
            PaymentLine_PropertyChanged;

        PaymentLines.Add(
            payment);

        RefreshPaymentSummary();
    }

    public void RemovePayment(
        PosPaymentLineViewModel payment)
    {
        payment.PropertyChanged -=
            PaymentLine_PropertyChanged;

        PaymentLines.Remove(
            payment);

        RefreshPaymentSummary();
    }

    private void PaymentLine_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
                nameof(
                    PosPaymentLineViewModel.Amount) ||
            e.PropertyName ==
                nameof(
                    PosPaymentLineViewModel.SelectedPaymentMethod) ||
            e.PropertyName ==
                nameof(
                    PosPaymentLineViewModel.ReferenceNumber))
        {
            RefreshPaymentSummary();
        }
    }

    private void RefreshPaymentSummary()
    {
        OnPropertyChanged(
            nameof(PaidAmount));

        OnPropertyChanged(
            nameof(DueAmount));

        OnPropertyChanged(
            nameof(HasDue));

        OnPropertyChanged(
            nameof(HasPayments));

        OnPropertyChanged(
            nameof(FormattedPaidAmount));

        OnPropertyChanged(
            nameof(FormattedDueAmount));

        OnPropertyChanged(
            nameof(PaymentStatusDisplay));

        OnPropertyChanged(
            nameof(CanUseCredit));
    }

    /*
     * Preserve the convenient cash checkout behavior
     * without overwriting an intentional split payment.
     *
     * If the cart was fully paid before its total changed,
     * a single payment line follows the new total.
     *
     * Multiple payment lines are never silently changed.
     */
    private void UpdatePaymentsAfterCartChange(
        decimal previousTotal)
    {
        if (CartLines.Count == 0)
        {
            ClearPayments();
            return;
        }

        if (PaymentLines.Count == 0)
        {
            AddPayment();
            return;
        }

        if (PaymentLines.Count != 1)
            return;

        var payment =
            PaymentLines[0];

        var previousPaid =
            payment.DecimalAmount;

        if (previousPaid == previousTotal)
        {
            payment.Amount =
                Convert.ToDouble(
                    Total);

            return;
        }

        if (previousPaid > Total)
        {
            payment.Amount =
                Convert.ToDouble(
                    Total);
        }
    }

    private void ClearPayments()
    {
        foreach (var payment in PaymentLines)
        {
            payment.PropertyChanged -=
                PaymentLine_PropertyChanged;
        }

        PaymentLines.Clear();

        RefreshPaymentSummary();
    }

    // ---------------------------------------------------------
    // Load an accepted quotation into POS (Q8)
    // ---------------------------------------------------------

    public async Task<bool> LoadQuoteAsync(Guid quoteId)
    {
        if (IsSaving || IsLoading)
        {
            ErrorMessage = "POS is busy. Please try again.";
            return false;
        }

        if (CartLines.Count > 0 || HasLoadedQuote)
        {
            ErrorMessage = "Clear the existing sale before loading a quotation.";
            return false;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var quote = await _salesQuoteService.GetByIdAsync(quoteId);
            if (quote is null)
                throw new InvalidOperationException("Quotation not found.");

            if (quote.Status != SalesQuoteStatus.Accepted || quote.SaleId.HasValue)
                throw new InvalidOperationException("Only unconverted accepted quotations can be loaded.");

            if (quote.ValidUntilDate.HasValue &&
                quote.ValidUntilDate.Value.Date < DateTime.UtcNow.Date)
                throw new InvalidOperationException("This quotation has expired.");

            // The current POS UI supports price × quantity only.
            if (quote.DiscountAmount != 0 || quote.TaxAmount != 0 ||
                quote.Lines.Any(x => x.DiscountAmount != 0 || x.TaxAmount != 0))
                throw new InvalidOperationException(
                    "Quotations containing discounts or taxes require the extended POS checkout UI.");

            var location = Locations.FirstOrDefault(x => x.Id == quote.LocationId)
                ?? throw new InvalidOperationException("The quotation's location is unavailable.");

            PosCustomerDto? customer = null;
            if (quote.CustomerId.HasValue)
            {
                customer = Customers.FirstOrDefault(x => x.Id == quote.CustomerId.Value)
                    ?? throw new InvalidOperationException("The quotation's customer is unavailable.");
            }

            if (SelectedLocation?.Id != location.Id)
            {
                SelectedLocation = location;
                await _productLoadTask;
            }
            else
            {
                await _productLoadTask;
            }

            if (!string.IsNullOrWhiteSpace(ErrorMessage))
                throw new InvalidOperationException(ErrorMessage);

            var imported = new List<PosCartLineViewModel>();
            foreach (var quotedLine in quote.Lines)
            {
                var product = Products.FirstOrDefault(
                    x => x.ProductVariantId == quotedLine.ProductVariantId)
                    ?? throw new InvalidOperationException(
                        $"Product {quotedLine.SKU} is unavailable at this location.");

                if (quotedLine.Quantity <= 0 ||
                    quotedLine.Quantity > product.QuantityAvailable)
                    throw new InvalidOperationException(
                        $"Insufficient stock for {quotedLine.SKU}. " +
                        $"Required: {quotedLine.Quantity:N3}; " +
                        $"Available: {product.QuantityAvailable:N3}.");

                imported.Add(new PosCartLineViewModel(product, quotedLine.UnitPrice)
                {
                    Quantity = Convert.ToDouble(quotedLine.Quantity)
                });
            }

            SelectedCustomer = customer;
            foreach (var line in imported)
            {
                line.PropertyChanged += CartLine_PropertyChanged;
                CartLines.Add(line);
            }

            _loadedQuoteId = quote.Id;
            _loadedQuoteNumber = quote.QuoteNumber;
            NotifyQuoteStateChanged();
            RefreshTotals();

            if (PaymentLines.Count == 0 && Total > 0)
                AddPayment();

            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NotifyQuoteStateChanged()
    {
        OnPropertyChanged(nameof(LoadedQuoteId));
        OnPropertyChanged(nameof(LoadedQuoteNumber));
        OnPropertyChanged(nameof(HasLoadedQuote));
    }

    // ---------------------------------------------------------
    // Create Quotation
    // ---------------------------------------------------------

    public async Task<SalesQuoteResultDto?>
        CreateQuoteAsync(
            DateTime? validUntilDate,
            string? notes,
            string? terms)
    {
        ErrorMessage = null;

        if (IsSaving || HasLoadedQuote)
        {
            ErrorMessage = HasLoadedQuote
                ? "Finish the imported quotation instead of creating another quotation."
                : "POS is busy.";
            return null;
        }

        // -----------------------------------------------------
        // Location
        // -----------------------------------------------------

        if (SelectedLocation is null)
        {
            ErrorMessage =
                "Please select a location.";

            return null;
        }

        // -----------------------------------------------------
        // Cart
        // -----------------------------------------------------

        if (CartLines.Count == 0)
        {
            ErrorMessage =
                "Add at least one product to the cart.";

            return null;
        }

        foreach (var line in CartLines)
        {
            if (line.Quantity <= 0)
            {
                ErrorMessage =
                    $"Quantity must be greater than zero for {line.SKU}.";

                return null;
            }
        }

        try
        {
            IsSaving = true;
            ErrorMessage = null;

            var quoteDate =
                DateTime.UtcNow;

            var request =
                new CreateSalesQuoteRequest
                {
                    LocationId =
                        SelectedLocation.Id,

                    CustomerId =
                        SelectedCustomer?.Id,

                    QuoteDate =
                        quoteDate,

                    ValidUntilDate =
                        validUntilDate,

                    DiscountAmount =
                        0,

                    TaxAmount =
                        0,

                    Notes =
                        string.IsNullOrWhiteSpace(notes)
                            ? null
                            : notes.Trim(),

                    Terms =
                        string.IsNullOrWhiteSpace(terms)
                            ? null
                            : terms.Trim(),

                    Lines =
                        CartLines
                            .Select(
                                line =>
                                    new CreateSalesQuoteLineRequest
                                    {
                                        ProductVariantId =
                                            line.ProductVariantId,

                                        Quantity =
                                            Convert.ToDecimal(
                                                line.Quantity),

                                        UnitPrice =
                                            line.UnitPrice,

                                        DiscountAmount =
                                            0,

                                        TaxAmount =
                                            0,

                                        WarrantyMonths =
                                            null
                                    })
                            .ToList()
                };

            return await _salesQuoteService
                .CreateAsync(request);
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;

            return null;
        }
        finally
        {
            IsSaving = false;
        }
    }

    // ---------------------------------------------------------
    // Complete Sale
    // ---------------------------------------------------------

    public async Task<SaleResultDto?>
        CompleteSaleAsync()
    {
        if (IsSaving)
            return null;

        ErrorMessage = null;

        // -----------------------------------------------------
        // Location
        // -----------------------------------------------------

        if (SelectedLocation is null)
        {
            ErrorMessage =
                "Please select a location.";

            return null;
        }

        // -----------------------------------------------------
        // Cart
        // -----------------------------------------------------

        if (CartLines.Count == 0)
        {
            ErrorMessage =
                "Add at least one product to the cart.";

            return null;
        }

        foreach (var line in CartLines)
        {
            if (line.Quantity <= 0)
            {
                ErrorMessage =
                    $"Quantity must be greater than zero for {line.SKU}.";

                return null;
            }

            if (Convert.ToDecimal(
                    line.Quantity) >
                line.QuantityAvailable)
            {
                ErrorMessage =
                    $"Insufficient stock for {line.SKU}. " +
                    $"Only {line.QuantityAvailable:N3} " +
                    $"{line.UnitSymbol} available.";

                return null;
            }
        }

        // -----------------------------------------------------
        // Payment lines
        // -----------------------------------------------------

        foreach (var payment in PaymentLines)
        {
            if (double.IsNaN(
                    payment.Amount) ||
                double.IsInfinity(
                    payment.Amount))
            {
                ErrorMessage =
                    "Please enter a valid payment amount.";

                return null;
            }

            if (payment.Amount <= 0)
            {
                ErrorMessage =
                    "Each payment amount must be greater than zero.";

                return null;
            }

            if (payment.RequiresReference &&
                string.IsNullOrWhiteSpace(
                    payment.ReferenceNumber))
            {
                ErrorMessage =
                    $"{payment.ReferenceLabel} is required.";

                return null;
            }
        }

        var paid =
            PaidAmount;

        var total =
            Total;

        if (paid > total)
        {
            ErrorMessage =
                "Payment amount cannot exceed the sale total.";

            return null;
        }

        var due =
            total - paid;

        // Walk-in customer cannot owe money.

        if (due > 0 &&
            SelectedCustomer is null)
        {
            ErrorMessage =
                "Please select a customer for a partial or credit sale.";

            return null;
        }

        // Client-side credit-limit check.
        // SaleService also performs authoritative validation.

        if (due > 0 &&
            SelectedCustomer is not null &&
            due >
                SelectedCustomer.AvailableCredit)
        {
            ErrorMessage =
                $"This sale would exceed the customer's " +
                $"available credit of " +
                $"{SelectedCustomer.FormattedAvailableCredit}.";

            return null;
        }

        try
        {
            IsSaving = true;
            ErrorMessage = null;

            // -------------------------------------------------
            // Build payments
            // -------------------------------------------------

            var payments =
                PaymentLines
                    .Select(
                        payment =>
                            new CreateSalePaymentRequest
                            {
                                PaymentMethod =
                                    payment.SelectedPaymentMethod,

                                Amount =
                                    payment.DecimalAmount,

                                ReferenceNumber =
                                    payment.RequiresReference
                                        ? payment.ReferenceNumber.Trim()
                                        : null
                            })
                    .ToList();

            // -------------------------------------------------
            // Build sale
            // -------------------------------------------------

            var request =
                new CreateSaleRequest
                {
                    SalesQuoteId =
                        LoadedQuoteId,

                    LocationId =
                        SelectedLocation.Id,

                    CustomerId =
                        SelectedCustomer?.Id,

                    OrderDiscountAmount =
                        0,

                    TaxAmount =
                        0,

                    Lines =
                        CartLines
                            .Select(
                                line =>
                                    new CreateSaleLineRequest
                                    {
                                        ProductVariantId =
                                            line.ProductVariantId,

                                        Quantity =
                                            Convert.ToDecimal(
                                                line.Quantity),

                                        UnitPrice =
                                            line.UnitPrice,

                                        DiscountAmount =
                                            0,

                                        TaxAmount =
                                            0
                                    })
                            .ToList(),

                    Payments =
                        payments
                };

            // -------------------------------------------------
            // Create sale
            // -------------------------------------------------

            var result =
                await _saleService
                    .CreateSaleAsync(
                        request);

            // The service committed the sale and quotation together.
            _loadedQuoteId = null;
            _loadedQuoteNumber = null;
            NotifyQuoteStateChanged();

            LastInvoiceNumber =
                result.InvoiceNumber;

            // -------------------------------------------------
            // Clear cart
            // -------------------------------------------------

            foreach (var line in CartLines)
            {
                line.PropertyChanged -=
                    CartLine_PropertyChanged;
            }

            CartLines.Clear();

            ClearPayments();

            RefreshTotals();

            // -------------------------------------------------
            // Refresh customer credit
            // -------------------------------------------------

            try
            {
                var selectedCustomerId =
                    SelectedCustomer?.Id;

                var customers =
                    await _saleService
                        .GetCustomersAsync();

                Customers.Clear();

                foreach (var customer in customers)
                {
                    Customers.Add(
                        customer);
                }

                if (selectedCustomerId.HasValue)
                {
                    SelectedCustomer =
                        Customers.FirstOrDefault(
                            x =>
                                x.Id ==
                                selectedCustomerId.Value);
                }
                else
                {
                    SelectedCustomer = null;
                }

                // -------------------------------------------------
                // Refresh inventory
                // -------------------------------------------------

                await LoadProductsAsync();
            }
            catch (Exception refreshError)
            {
                // Sale already committed. Never report checkout failure here.
                ErrorMessage = "Sale completed, but POS refresh failed: " + refreshError.Message;
            }

            return result;
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;

            return null;
        }
        finally
        {
            IsSaving = false;
        }
    }

    // ---------------------------------------------------------
    // Walk-in
    // ---------------------------------------------------------

    public void SelectWalkInCustomer()
    {
        SelectedCustomer = null;
    }

    // ---------------------------------------------------------
    // INotifyPropertyChanged
    // ---------------------------------------------------------

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