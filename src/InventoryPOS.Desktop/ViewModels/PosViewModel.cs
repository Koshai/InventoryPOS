using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Application.Pricing;
using InventoryPOS.Domain.Enums;
using InventoryPOS.Desktop.Services;
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
    private readonly IProductBarcodeService _productBarcodeService;
    private readonly IBusinessProfileService _businessProfileService;
    private readonly IPricingCalculator _pricingCalculator;
    private readonly IPendingCheckoutJournalService _checkoutJournal;
    private bool _checkoutJournalLoadFailed;

    private LocationLookupDto? _selectedLocation;
    private PosCustomerDto? _selectedCustomer;

    private string _searchText =
        string.Empty;

    private bool _isLoading;
    private bool _isSaving;
    private CreateSaleRequest? _pendingCheckoutRequest;
    private bool _checkoutOutcomeUncertain;

    public bool HasPendingCheckout => _pendingCheckoutRequest is not null;
    public bool CheckoutOutcomeUncertain => _checkoutOutcomeUncertain;

    private string? _errorMessage;
    private string? _lastInvoiceNumber;
    private Task _productLoadTask = Task.CompletedTask;
    private Guid? _loadedQuoteId;
    private string? _loadedQuoteNumber;

    public Guid? LoadedQuoteId => _loadedQuoteId;
    public string? LoadedQuoteNumber => _loadedQuoteNumber;
    private SalesQuoteDetailsDto? _loadedQuotePricing;
    public bool HasLoadedQuote => _loadedQuoteId.HasValue;

    private bool _enableLineDiscount;
    private bool _enableOrderDiscount;
    private bool _enableTax;
    private bool _enableTransportCharge;

    private decimal _defaultTaxRate;

    private PricingCalculationResult? _pricingResult;

    private PricingAdjustmentType _orderDiscountType =
    PricingAdjustmentType.FixedAmount;

    private double _orderDiscountValue;

    private double _transportAmount;

    public bool CanEditCart => !HasLoadedQuote && !HasPendingCheckout && !_checkoutJournalLoadFailed;

    public bool CanCreateQuote => HasItems && !HasLoadedQuote && !HasPendingCheckout && !_checkoutJournalLoadFailed;

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

    public PricingAdjustmentType OrderDiscountType
    {
        get => _orderDiscountType;

        set
        {
            if (_orderDiscountType == value)
                return;

            // Prevent editing imported quotation pricing
            if (HasPendingCheckout)
            {
                ErrorMessage = "Resolve the pending checkout before editing pricing.";
                return;
            }
            if (HasLoadedQuote)
            {
                ErrorMessage =
                    "Pricing is locked for an accepted quotation.";
                return;
            }

            _orderDiscountType = value;
            OnPropertyChanged();

            RecalculateAfterPricingChange();
        }
    }

    public double OrderDiscountValue
    {
        get => _orderDiscountValue;

        set
        {
            if (_orderDiscountValue == value)
                return;

            // Prevent editing imported quotation pricing
            if (HasPendingCheckout)
            {
                ErrorMessage = "Resolve the pending checkout before editing pricing.";
                return;
            }
            if (HasLoadedQuote)
            {
                ErrorMessage =
                    "Pricing is locked for an accepted quotation.";
                return;
            }

            _orderDiscountValue = value;
            OnPropertyChanged();

            RecalculateAfterPricingChange();
        }
    }

    public double TransportCharge
    {
        get => _transportAmount;

        set
        {
            if (_transportAmount == value)
                return;

            // Prevent editing imported quotation pricing
            if (HasPendingCheckout)
            {
                ErrorMessage = "Resolve the pending checkout before editing pricing.";
                return;
            }
            if (HasLoadedQuote)
            {
                ErrorMessage =
                    "Pricing is locked for an accepted quotation.";
                return;
            }

            _transportAmount = value;
            OnPropertyChanged();

            RecalculateAfterPricingChange();
        }
    }

    public IReadOnlyList<PricingAdjustmentType>
    DiscountTypes
    { get; } =
[
    PricingAdjustmentType.FixedAmount,
    PricingAdjustmentType.Percentage
];

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

            if (RejectImportedQuoteEdit())
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

            if (RejectImportedQuoteEdit())
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
    // Sales Pricing Configuration
    // ---------------------------------------------------------

    public bool EnableLineDiscount =>
        _enableLineDiscount;

    public bool EnableOrderDiscount =>
        _enableOrderDiscount;

    public bool EnableTax =>
        _enableTax;

    public bool EnableTransportCharge =>
        _enableTransportCharge;

    public decimal DefaultTaxRate =>
        _defaultTaxRate;

    public bool ShowLineDiscount =>
        EnableLineDiscount || (HasLoadedQuote && LineDiscountAmount != 0m);

    public bool ShowOrderDiscount =>
        EnableOrderDiscount || (HasLoadedQuote && OrderDiscountAmount != 0m);

    public bool ShowTax =>
        EnableTax || (HasLoadedQuote && TaxAmount != 0m);

    public bool ShowTransport =>
        EnableTransportCharge || (HasLoadedQuote && TransportAmount != 0m);

    public string TaxDisplay =>
        HasLoadedQuote && _loadedQuotePricing?.PricingVersion == 2
            ? $"Tax / VAT ({_loadedQuotePricing.SalesTaxRate:N2}%)"
            : EnableTax
                ? $"Tax / VAT ({DefaultTaxRate:N2}%)"
                : "Tax / VAT";

    // ---------------------------------------------------------
    // Totals
    // ---------------------------------------------------------

    public decimal GrossAmount =>
    _pricingResult?.GrossAmount ?? 0m;

    public decimal LineDiscountAmount =>
        _pricingResult?.LineDiscountAmount ?? 0m;

    public decimal Subtotal =>
        _pricingResult?.Subtotal ?? 0m;

    public decimal OrderDiscountAmount =>
        _pricingResult?.OrderDiscountAmount ?? 0m;

    public decimal DiscountAmount =>
        _pricingResult?.DiscountAmount ?? 0m;

    public decimal TaxAmount =>
        _pricingResult?.TaxAmount ?? 0m;

    public decimal TransportAmount =>
        _pricingResult?.TransportAmount ?? 0m;

    public decimal Total =>
        _pricingResult?.TotalAmount ?? 0m;

    public string FormattedSubtotal =>
        $"৳{Subtotal:N2}";

    public string FormattedTotal =>
        $"৳{Total:N2}";

    public bool HasItems =>
        CartLines.Count > 0;

    public string FormattedGrossAmount =>
    $"৳{GrossAmount:N2}";

    public string FormattedLineDiscountAmount =>
        $"৳{LineDiscountAmount:N2}";

    public string FormattedOrderDiscountAmount =>
        $"৳{OrderDiscountAmount:N2}";

    public string FormattedDiscountAmount =>
        $"৳{DiscountAmount:N2}";

    public string FormattedTaxAmount =>
        $"৳{TaxAmount:N2}";

    public string FormattedTransportAmount =>
        $"৳{TransportAmount:N2}";

    // ---------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------

    public PosViewModel(
    ISaleService saleService,
    ISalesQuoteService salesQuoteService,
    IProductBarcodeService productBarcodeService,
    IBusinessProfileService businessProfileService,
    IPricingCalculator pricingCalculator,
    IPendingCheckoutJournalService? checkoutJournal = null)
    {
        _checkoutJournal = checkoutJournal ?? new PendingCheckoutJournalService();
        _saleService =
            saleService;

        _salesQuoteService =
            salesQuoteService;

        _productBarcodeService =
            productBarcodeService;

        _businessProfileService =
            businessProfileService;

        _pricingCalculator =
            pricingCalculator;
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

            // Fail closed: an unreadable journal must not permit a new sale.
            try
            {
                var recovered = await _checkoutJournal.LoadAsync();
                if (recovered is not null)
                {
                    SetPendingCheckout(recovered, true);
                    ErrorMessage = "An unfinished checkout was recovered. Use Complete Sale to retry the original request.";
                }
            }
            catch (Exception journalError)
            {
                _checkoutJournalLoadFailed = true;
                ErrorMessage = "Checkout recovery journal could not be read. New checkout is blocked: " + journalError.Message;
            }

            await LoadPricingSettingsAsync();

            RefreshTotals();

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

            // Reconcile a recovered journal without resubmitting the sale.
            // Any lookup failure leaves the original request safely pending.
            if (_pendingCheckoutRequest is not null && !_checkoutJournalLoadFailed)
            {
                try
                {
                    var committed = await _saleService.FindCommittedCheckoutAsync(
                        _pendingCheckoutRequest);
                    if (committed is not null)
                    {
                        await HandleCompletedSaleAsync(committed);
                    }
                    else
                    {
                        ErrorMessage =
                            "Recovered checkout was not found in the database. " +
                            "Review it and use Complete Sale to retry the original request.";
                    }
                }
                catch (Exception reconciliationError)
                {
                    ErrorMessage =
                        "Checkout reconciliation could not be completed. " +
                        "The original request remains locked for recovery: " +
                        reconciliationError.Message;
                }
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

    private async Task LoadPricingSettingsAsync()
    {
        var profile =
            await _businessProfileService.GetAsync();

        if (profile is null)
        {
            _enableLineDiscount = true;
            _enableOrderDiscount = true;
            _enableTax = false;
            _defaultTaxRate = 0m;
            _enableTransportCharge = false;
        }
        else
        {
            _enableLineDiscount =
                profile.EnableSalesLineDiscount;

            _enableOrderDiscount =
                profile.EnableSalesOrderDiscount;

            _enableTax =
                profile.EnableSalesTax;

            _defaultTaxRate =
                profile.DefaultSalesTaxRate;

            _enableTransportCharge =
                profile.EnableSalesTransportCharge;
        }

        OnPropertyChanged(nameof(EnableLineDiscount));
        OnPropertyChanged(nameof(EnableOrderDiscount));
        OnPropertyChanged(nameof(EnableTax));
        OnPropertyChanged(nameof(EnableTransportCharge));
        OnPropertyChanged(nameof(DefaultTaxRate));
        OnPropertyChanged(nameof(TaxDisplay));
        NotifyPricingVisibilityChanged();
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
    // Barcode Scanning
    // ---------------------------------------------------------

    public async Task<bool> ScanBarcodeAsync(
        string barcode)
    {
        if (RejectImportedQuoteEdit())
            return false;

        ErrorMessage = null;

        var normalized =
            barcode?.Trim();

        if (string.IsNullOrWhiteSpace(
                normalized))
        {
            ErrorMessage =
                "Scan or enter a barcode.";

            return false;
        }

        if (SelectedLocation is null)
        {
            ErrorMessage =
                "Please select a location.";

            return false;
        }

        try
        {
            // Make sure the product collection for the currently
            // selected location has finished loading.
            await _productLoadTask;

            if (!string.IsNullOrWhiteSpace(
                    ErrorMessage))
            {
                return false;
            }

            // Barcode only identifies the variant.
            var resolved =
                await _productBarcodeService
                    .ResolveAsync(
                        normalized);

            if (resolved is null)
            {
                ErrorMessage =
                    $"Barcode '{normalized}' was not found.";

                return false;
            }

            // The existing POS product collection remains
            // authoritative for price and stock at this location.
            var product =
                Products.FirstOrDefault(
                    x =>
                        x.ProductVariantId ==
                        resolved.ProductVariantId);

            if (product is null)
            {
                ErrorMessage =
                    "This barcode belongs to a product that is " +
                    "not available at the selected location.";

                return false;
            }

            // Reuse the existing cart logic.
            //
            // AddProduct already:
            // - checks stock
            // - creates a new cart line
            // - increments an existing line
            // - checks maximum available quantity
            // - updates totals
            // - updates payment state
            AddProduct(
                product);

            return string.IsNullOrWhiteSpace(
                ErrorMessage);
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;

            return false;
        }
    }

    // ---------------------------------------------------------
    // Cart
    // ---------------------------------------------------------

    public void AddProduct(
        PosProductDto product)
    {
        if (RejectImportedQuoteEdit())
            return;

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

        if (RejectImportedQuoteEdit())
            return;

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
        if (RejectImportedQuoteEdit())
            return;

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
                nameof(PosCartLineViewModel.Quantity) ||
            e.PropertyName ==
                nameof(PosCartLineViewModel.DiscountType) ||
            e.PropertyName ==
                nameof(PosCartLineViewModel.DiscountValue))
        {
            var previousTotal =
                Total;

            RefreshTotals();

            UpdatePaymentsAfterCartChange(
                previousTotal);
        }
    }

    private void RefreshTotals()
    {
        try
        {
            var importedQuote = _loadedQuotePricing;

            var isCalculatorQuote =
                importedQuote is not null &&
                importedQuote.PricingVersion == 2;

            var request = new PricingCalculationRequest
            {
                Lines = CartLines
                    .Select(line =>
                    {
                        var quoteLine = isCalculatorQuote
                            ? importedQuote!.Lines.FirstOrDefault(
                                x => x.ProductVariantId == line.ProductVariantId)
                            : null;

                        return new PricingLineRequest
                        {
                            ProductVariantId = line.ProductVariantId,

                            Quantity = Convert.ToDecimal(line.Quantity),

                            UnitPrice = line.UnitPrice,

                            DiscountType = isCalculatorQuote
                                ? (PricingAdjustmentType)quoteLine!.DiscountType
                                : EnableLineDiscount
                                    ? line.DiscountType
                                    : PricingAdjustmentType.FixedAmount,

                            DiscountValue = isCalculatorQuote
                                ? quoteLine!.DiscountValue
                                : EnableLineDiscount
                                    ? line.DecimalDiscountValue
                                    : 0m,

                            TaxRate = isCalculatorQuote
                                ? quoteLine!.TaxRate
                                : 0m
                        };
                    })
                    .ToList(),

                OrderDiscountType = isCalculatorQuote
                    ? (PricingAdjustmentType)importedQuote!.OrderDiscountType
                    : EnableOrderDiscount
                        ? OrderDiscountType
                        : PricingAdjustmentType.FixedAmount,

                OrderDiscountValue = isCalculatorQuote
                    ? importedQuote!.OrderDiscountValue
                    : EnableOrderDiscount
                        ? Convert.ToDecimal(OrderDiscountValue)
                        : 0m,

                TaxRate = isCalculatorQuote
                    ? importedQuote!.SalesTaxRate
                    : EnableTax
                        ? DefaultTaxRate
                        : 0m,

                TransportAmount = isCalculatorQuote
                    ? importedQuote!.TransportAmount
                    : EnableTransportCharge
                        ? Convert.ToDecimal(TransportCharge)
                        : 0m
            };

            _pricingResult = _pricingCalculator.Calculate(request);

            if (isCalculatorQuote)
            {
                ValidateImportedQuotePricing(importedQuote!, _pricingResult);
            }
        }
        catch (Exception ex)
        {
            _pricingResult = null;
            ErrorMessage = ex.Message;
        }

        OnPropertyChanged(nameof(GrossAmount));
        OnPropertyChanged(nameof(LineDiscountAmount));

        OnPropertyChanged(nameof(Subtotal));

        OnPropertyChanged(nameof(OrderDiscountAmount));
        OnPropertyChanged(nameof(DiscountAmount));

        OnPropertyChanged(nameof(TaxAmount));
        OnPropertyChanged(nameof(TransportAmount));

        OnPropertyChanged(nameof(Total));

        OnPropertyChanged(nameof(FormattedGrossAmount));
        OnPropertyChanged(nameof(FormattedLineDiscountAmount));

        OnPropertyChanged(nameof(FormattedSubtotal));

        OnPropertyChanged(nameof(FormattedOrderDiscountAmount));
        OnPropertyChanged(nameof(FormattedDiscountAmount));

        OnPropertyChanged(nameof(FormattedTaxAmount));
        OnPropertyChanged(nameof(FormattedTransportAmount));

        OnPropertyChanged(nameof(FormattedTotal));

        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(CanCreateQuote));
        NotifyPricingVisibilityChanged();
        OnPropertyChanged(nameof(TaxDisplay));

        RefreshPaymentSummary();
    }

    // ---------------------------------------------------------
    // Payment Lines
    // ---------------------------------------------------------

    public void AddPayment()
    {
        if (RejectPendingCheckoutEdit()) return;
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
        if (RejectPendingCheckoutEdit()) return;
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
        if (IsSaving || IsLoading || HasPendingCheckout)
        {
            ErrorMessage = "POS is busy or has an unresolved checkout. Please try again.";
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

            if (quote.PricingVersion != 1 &&
    quote.PricingVersion != 2)
            {
                throw new InvalidOperationException(
                    $"Unsupported quotation pricing version: {quote.PricingVersion}.");
            }

            if (quote.PricingVersion == 1 &&
                (quote.DiscountAmount != 0m ||
                 quote.TaxAmount != 0m ||
                 quote.Lines.Any(x =>
                     x.DiscountAmount != 0m ||
                     x.TaxAmount != 0m)))
            {
                throw new InvalidOperationException(
                    "This legacy quotation contains pricing adjustments " +
                    "that require the legacy conversion path.");
            }

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

                var importedLine =
    new PosCartLineViewModel(product, quotedLine.UnitPrice)
    {
        Quantity = Convert.ToDouble(quotedLine.Quantity)
    };

                if (quote.PricingVersion == 2)
                {
                    importedLine.DiscountType =
                        (PricingAdjustmentType)quotedLine.DiscountType;

                    importedLine.DiscountValue =
                        Convert.ToDouble(quotedLine.DiscountValue);
                }

                imported.Add(importedLine);
            }

            SelectedCustomer = customer;
            foreach (var line in imported)
            {
                line.PropertyChanged += CartLine_PropertyChanged;
                CartLines.Add(line);
            }

            _loadedQuotePricing = quote;
            _loadedQuoteId = quote.Id;
            _loadedQuoteNumber = quote.QuoteNumber;

            if (quote.PricingVersion == 2)
            {
                _orderDiscountType =
                    (PricingAdjustmentType)quote.OrderDiscountType;

                _orderDiscountValue =
                    Convert.ToDouble(quote.OrderDiscountValue);

                _transportAmount =
                    Convert.ToDouble(quote.TransportAmount);

                OnPropertyChanged(nameof(OrderDiscountType));
                OnPropertyChanged(nameof(OrderDiscountValue));
                OnPropertyChanged(nameof(TransportCharge));
            }

            NotifyQuoteStateChanged();

            RefreshTotals();

            if (_pricingResult is null)
            {
                throw new InvalidOperationException(
                    ErrorMessage ?? "Unable to validate quotation pricing.");
            }

            if (quote.PricingVersion == 2 &&
                Total != quote.TotalAmount)
            {
                throw new InvalidOperationException(
                    "Imported quotation total does not match the saved total.");
            }

            if (PaymentLines.Count == 0 && Total > 0)
                AddPayment();

            return true;
        }
        catch (Exception ex)
        {
            foreach (var line in CartLines)
            {
                line.PropertyChanged -= CartLine_PropertyChanged;
            }

            CartLines.Clear();

            _loadedQuotePricing = null;
            _loadedQuoteId = null;
            _loadedQuoteNumber = null;

            _orderDiscountType = PricingAdjustmentType.FixedAmount;
            _orderDiscountValue = 0;
            _transportAmount = 0;

            NotifyQuoteStateChanged();
            RefreshTotals();
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
        OnPropertyChanged(nameof(CanEditCart));
        OnPropertyChanged(nameof(CanCreateQuote));
        NotifyPricingVisibilityChanged();
        OnPropertyChanged(nameof(TaxDisplay));
    }

    private void NotifyPricingVisibilityChanged()
    {
        OnPropertyChanged(nameof(ShowLineDiscount));
        OnPropertyChanged(nameof(ShowOrderDiscount));
        OnPropertyChanged(nameof(ShowTax));
        OnPropertyChanged(nameof(ShowTransport));
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

        if (IsSaving || HasLoadedQuote || HasPendingCheckout)
        {
            ErrorMessage = HasLoadedQuote
                ? "Finish the imported quotation instead of creating another quotation."
                : "POS is busy.";
            return null;
        }

        RefreshTotals();

        if (_pricingResult is null)
        {
            ErrorMessage = "Invalid quotation pricing.";
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

            var pricing = _pricingResult
    ?? throw new InvalidOperationException(
        "Quotation pricing is unavailable.");

            var calculatedLines = pricing.Lines.ToList();

            if (calculatedLines.Count != CartLines.Count)
            {
                throw new InvalidOperationException(
                    "Quotation pricing lines do not match the cart.");
            }

            var quoteLines = CartLines
                .Select((line, index) =>
                {
                    var calculated = calculatedLines[index];

                    if (calculated.ProductVariantId != line.ProductVariantId)
                    {
                        throw new InvalidOperationException(
                            "Quotation pricing product mismatch.");
                    }

                    return new CreateSalesQuoteLineRequest
                    {
                        ProductVariantId = line.ProductVariantId,

                        Quantity = Convert.ToDecimal(line.Quantity),

                        UnitPrice = line.UnitPrice,

                        DiscountType = (int)(
                            EnableLineDiscount
                                ? line.DiscountType
                                : PricingAdjustmentType.FixedAmount),

                        DiscountValue = EnableLineDiscount
                            ? line.DecimalDiscountValue
                            : 0m,

                        DiscountAmount = calculated.DiscountAmount,

                        TaxRate = 0m,

                        TaxAmount = calculated.TaxAmount,

                        WarrantyMonths = null
                    };
                })
                .ToList();

            var request = new CreateSalesQuoteRequest
            {
                LocationId = SelectedLocation.Id,

                CustomerId = SelectedCustomer?.Id,

                QuoteDate = quoteDate,

                ValidUntilDate = validUntilDate,

                UsePricingCalculator = true,

                OrderDiscountType = (int)(
                    EnableOrderDiscount
                        ? OrderDiscountType
                        : PricingAdjustmentType.FixedAmount),

                OrderDiscountValue = EnableOrderDiscount
                    ? Convert.ToDecimal(OrderDiscountValue)
                    : 0m,

                OrderDiscountAmount = pricing.OrderDiscountAmount,

                // Combined line + order discount.
                DiscountAmount = pricing.DiscountAmount,

                SalesTaxRate = EnableTax
                    ? DefaultTaxRate
                    : 0m,

                TaxAmount = pricing.TaxAmount,

                TransportAmount = pricing.TransportAmount,

                Notes = string.IsNullOrWhiteSpace(notes)
                    ? null
                    : notes.Trim(),

                Terms = string.IsNullOrWhiteSpace(terms)
                    ? null
                    : terms.Trim(),

                Lines = quoteLines
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

        if (_checkoutJournalLoadFailed)
        {
            ErrorMessage = "Checkout recovery is blocked because the journal could not be read.";
            return null;
        }

        if (_pendingCheckoutRequest is not null)
            return await RetryPendingCheckoutAsync();

        ErrorMessage = null;

        // Recalculate immediately before checkout.
        RefreshTotals();

        if (_pricingResult is null)
        {
            ErrorMessage =
                "Sale pricing could not be calculated. " +
                "Please check the discount and tax values.";

            return null;
        }

        // Quote conversion must retain its original pricing terms.
        // Until DT4, prevent current POS pricing settings from
        // silently changing the amount on an accepted quotation.
        if (HasLoadedQuote)
        {
            if (_loadedQuotePricing is null)
            {
                ErrorMessage = "The imported quotation snapshot is missing.";
                return null;
            }

            if (_loadedQuotePricing.PricingVersion == 2)
            {
                try
                {
                    ValidateImportedQuotePricing(
                        _loadedQuotePricing,
                        _pricingResult);
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    return null;
                }
            }
            else if (DiscountAmount != 0m ||
                     TaxAmount != 0m ||
                     TransportAmount != 0m)
            {
                ErrorMessage =
                    "Adjusted legacy quotations require the legacy conversion path.";
                return null;
            }
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

            var pricing = _pricingResult
    ?? throw new InvalidOperationException(
        "Sale pricing is unavailable.");

            var calculatedLines = pricing.Lines.ToList();

            if (calculatedLines.Count != CartLines.Count)
            {
                throw new InvalidOperationException(
                    "Calculated sale lines do not match the cart.");
            }

            var calculatorQuote =
    HasLoadedQuote &&
    _loadedQuotePricing?.PricingVersion == 2;

            var usePricingCalculator =
                !HasLoadedQuote || calculatorQuote;

            var saleLines = CartLines
                .Select((line, index) =>
                {
                    var calculated = calculatedLines[index];

                    if (calculated.ProductVariantId !=
                        line.ProductVariantId)
                    {
                        throw new InvalidOperationException(
                            "Calculated product order does not match the cart.");
                    }

                    return new CreateSaleLineRequest
                    {
                        ProductVariantId = line.ProductVariantId,

                        Quantity = Convert.ToDecimal(line.Quantity),

                        UnitPrice = line.UnitPrice,

                        DiscountType = calculatorQuote
    ? _loadedQuotePricing!.Lines.Single(x =>
        x.ProductVariantId == line.ProductVariantId).DiscountType
    : (int)(
        EnableLineDiscount
            ? line.DiscountType
            : PricingAdjustmentType.FixedAmount),

                        DiscountValue = calculatorQuote
    ? _loadedQuotePricing!.Lines.Single(x =>
        x.ProductVariantId == line.ProductVariantId).DiscountValue
    : EnableLineDiscount
        ? line.DecimalDiscountValue
        : 0m,

                        DiscountAmount = calculated.DiscountAmount,

                        TaxRate = calculatorQuote
    ? _loadedQuotePricing!.Lines.Single(x =>
        x.ProductVariantId == line.ProductVariantId).TaxRate
    : 0m,

                        TaxAmount = calculated.TaxAmount
                    };
                })
                .ToList();

            var request = new CreateSaleRequest
            {
                CheckoutRequestId = Guid.NewGuid(),
                UsePricingCalculator = usePricingCalculator,

                SalesQuoteId = LoadedQuoteId,

                LocationId = SelectedLocation.Id,

                CustomerId = SelectedCustomer?.Id,

                OrderDiscountType = calculatorQuote
    ? _loadedQuotePricing!.OrderDiscountType
    : (int)(
        EnableOrderDiscount
            ? OrderDiscountType
            : PricingAdjustmentType.FixedAmount),

                OrderDiscountValue = calculatorQuote
    ? _loadedQuotePricing!.OrderDiscountValue
    : EnableOrderDiscount
        ? Convert.ToDecimal(OrderDiscountValue)
        : 0m,

                OrderDiscountAmount = pricing.OrderDiscountAmount,

                SalesTaxRate = calculatorQuote
    ? _loadedQuotePricing!.SalesTaxRate
    : EnableTax
        ? DefaultTaxRate
        : 0m,

                TaxAmount = pricing.TaxAmount,

                TransportAmount = pricing.TransportAmount,

                Lines = saleLines,

                Payments = payments
            };

            // Persist before contacting PostgreSQL. If persistence fails,
            // no database submission occurs.
            await _checkoutJournal.SaveAsync(request);
            SetPendingCheckout(request, true);

            var result = await _saleService.CreateSaleAsync(request);
            await HandleCompletedSaleAsync(result);
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
    // Retry and confirmed checkout handling (DT6.1A)
    // ---------------------------------------------------------

    private async Task<SaleResultDto?> RetryPendingCheckoutAsync()
    {
        if (IsSaving || _pendingCheckoutRequest is null)
            return null;

        try
        {
            IsSaving = true;
            ErrorMessage = null;

            // Never rebuild a retry from the current UI state.
            var result = await _saleService.CreateSaleAsync(_pendingCheckoutRequest);
            await HandleCompletedSaleAsync(result);
            return result;
        }
        catch (Exception ex)
        {
            _checkoutOutcomeUncertain = true;
            OnPropertyChanged(nameof(CheckoutOutcomeUncertain));
            ErrorMessage = "Checkout outcome could not be confirmed. Retry the same checkout. " + ex.Message;
            return null;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task HandleCompletedSaleAsync(SaleResultDto result)
    {
        // Confirmation is authoritative. Do not unlock POS until the
        // durable journal has also been removed successfully.
        var confirmedRequest = _pendingCheckoutRequest
            ?? throw new InvalidOperationException("Confirmed checkout has no pending request.");
        await _checkoutJournal.ClearAsync(confirmedRequest.CheckoutRequestId);
        SetPendingCheckout(null, false);

        _loadedQuoteId = null;
        _loadedQuoteNumber = null;
        _loadedQuotePricing = null;
        NotifyQuoteStateChanged();
        LastInvoiceNumber = result.InvoiceNumber;

        foreach (var line in CartLines)
            line.PropertyChanged -= CartLine_PropertyChanged;
        CartLines.Clear();
        ClearPayments();
        RefreshTotals();

        try
        {
            var selectedCustomerId = SelectedCustomer?.Id;
            var customers = await _saleService.GetCustomersAsync();
            Customers.Clear();
            foreach (var customer in customers)
                Customers.Add(customer);

            SelectedCustomer = selectedCustomerId.HasValue
                ? Customers.FirstOrDefault(x => x.Id == selectedCustomerId.Value)
                : null;

            await LoadProductsAsync();
        }
        catch (Exception refreshError)
        {
            ErrorMessage = "Sale completed, but POS refresh failed: " + refreshError.Message;
        }
    }

    // ---------------------------------------------------------
    // Walk-in
    // ---------------------------------------------------------

    public void SelectWalkInCustomer()
    {
        if (RejectImportedQuoteEdit())
            return;

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

    private void RecalculateAfterPricingChange()
    {
        var previousTotal = Total;

        RefreshTotals();

        UpdatePaymentsAfterCartChange(
            previousTotal);
    }

    private static void ValidateImportedQuotePricing(
    SalesQuoteDetailsDto quote,
    PricingCalculationResult pricing)
    {
        if (quote.PricingVersion != 2)
            return;

        if (pricing.Lines.Count != quote.Lines.Count)
        {
            throw new InvalidOperationException(
                "The imported quotation's line count has changed.");
        }

        foreach (var quotedLine in quote.Lines)
        {
            var matches = pricing.Lines
                .Where(x =>
                    x.ProductVariantId == quotedLine.ProductVariantId)
                .ToList();

            if (matches.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Quotation line {quotedLine.SKU} is missing or duplicated.");
            }

            var calculatedLine = matches[0];

            if (calculatedLine.DiscountAmount != quotedLine.DiscountAmount ||
                calculatedLine.TaxAmount != quotedLine.TaxAmount ||
                calculatedLine.LineTotal != quotedLine.LineTotal)
            {
                throw new InvalidOperationException(
                    $"Pricing mismatch for {quotedLine.SKU}. " +
                    "The accepted quotation cannot be repriced.");
            }
        }

        if (pricing.GrossAmount != quote.Subtotal ||
            pricing.LineDiscountAmount != quote.LineDiscountAmount ||
            pricing.OrderDiscountAmount != quote.OrderDiscountAmount ||
            pricing.DiscountAmount != quote.DiscountAmount ||
            pricing.TaxAmount != quote.TaxAmount ||
            pricing.TransportAmount != quote.TransportAmount ||
            pricing.TotalAmount != quote.TotalAmount)
        {
            throw new InvalidOperationException(
                $"Quotation total mismatch. " +
                $"Saved: ৳{quote.TotalAmount:N2}; " +
                $"Calculated: ৳{pricing.TotalAmount:N2}. " +
                "The quotation cannot be repriced.");
        }
    }

    private void SetPendingCheckout(CreateSaleRequest? request, bool uncertain)
    {
        _pendingCheckoutRequest = request;
        _checkoutOutcomeUncertain = uncertain;
        OnPropertyChanged(nameof(HasPendingCheckout));
        OnPropertyChanged(nameof(CheckoutOutcomeUncertain));
        OnPropertyChanged(nameof(CanEditCart));
        OnPropertyChanged(nameof(CanCreateQuote));
    }

    private bool RejectPendingCheckoutEdit()
    {
        if (!HasPendingCheckout && !_checkoutJournalLoadFailed) return false;
        ErrorMessage = "Checkout recovery is pending. Resolve it before editing the sale.";
        return true;
    }

    private bool RejectImportedQuoteEdit()
    {
        if (RejectPendingCheckoutEdit()) return true;
        if (!HasLoadedQuote)
            return false;

        ErrorMessage =
            "This cart contains an accepted quotation. " +
            "Its products and pricing cannot be edited. " +
            "You may still enter payments.";

        return true;
    }
}