using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class SalesViewModel : INotifyPropertyChanged
{
    private readonly ISaleService _saleService;

    private string _searchText = string.Empty;
    private SaleStatus? _selectedStatus;
    private DateTimeOffset? _fromDate = DateTimeOffset.Now.Date;
    private DateTimeOffset? _toDate = DateTimeOffset.Now.Date;

    private int _pageNumber = 1;
    private const int PageSize = 20;
    private int _totalCount;
    private int _totalPages;

    private bool _isLoading;
    private string? _errorMessage;
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _debounceCts;
    private long _requestVersion;
    private bool _isResettingFilters;

    public bool IsResettingFilters =>
        _isResettingFilters;

    public SalesViewModel(ISaleService saleService)
    {
        _saleService = saleService;

        var today = DateTimeOffset.Now.Date;

        _fromDate = today;
        _toDate = today;

        StatusOptions = new ObservableCollection<SaleStatusOption>
    {
        new(null, "All Statuses"),
        new(SaleStatus.Completed, "Fully Paid"),
        new(SaleStatus.PartiallyPaid, "Partially Paid"),
        new(SaleStatus.Credit, "Credit"),
        new(SaleStatus.Draft, "Draft"),
        new(SaleStatus.Cancelled, "Cancelled"),
        new(SaleStatus.Refunded, "Refunded")
    };

        SelectedStatusOption = StatusOptions[0];
    }

    // =========================================================
    // COLLECTIONS
    // =========================================================

    public ObservableCollection<SalesHistoryDto> Sales { get; }
        = new();

    public ObservableCollection<SaleStatusOption> StatusOptions { get; }

    // =========================================================
    // FILTERS
    // =========================================================

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
                return;

            _searchText = value;
            OnPropertyChanged();
        }
    }

    private SaleStatusOption? _selectedStatusOption;

    public SaleStatusOption? SelectedStatusOption
    {
        get => _selectedStatusOption;
        set
        {
            if (_selectedStatusOption == value)
                return;

            _selectedStatusOption = value;
            _selectedStatus = value?.Status;

            OnPropertyChanged();
        }
    }

    public DateTimeOffset? FromDate
    {
        get => _fromDate;
        set
        {
            if (_fromDate == value)
                return;

            _fromDate = value;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset? ToDate
    {
        get => _toDate;
        set
        {
            if (_toDate == value)
                return;

            _toDate = value;
            OnPropertyChanged();
        }
    }

    // =========================================================
    // PAGINATION
    // =========================================================

    public int PageNumber
    {
        get => _pageNumber;
        private set
        {
            if (_pageNumber == value)
                return;

            _pageNumber = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(PageDisplay));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(ResultDisplay));
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        private set
        {
            if (_totalCount == value)
                return;

            _totalCount = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(ResultDisplay));
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        private set
        {
            if (_totalPages == value)
                return;

            _totalPages = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(PageDisplay));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
        }
    }

    public string PageDisplay =>
        TotalPages <= 0
            ? "Page 0 of 0"
            : $"Page {PageNumber} of {TotalPages}";

    public string ResultDisplay
    {
        get
        {
            if (TotalCount == 0)
                return "No sales found";

            var start =
                ((PageNumber - 1) * PageSize) + 1;

            var end =
                Math.Min(
                    PageNumber * PageSize,
                    TotalCount);

            return $"Showing {start:N0}–{end:N0} of {TotalCount:N0}";
        }
    }

    public bool CanGoPrevious =>
        !IsLoading &&
        PageNumber > 1;

    public bool CanGoNext =>
        !IsLoading &&
        TotalPages > 0 &&
        PageNumber < TotalPages;

    // =========================================================
    // STATE
    // =========================================================

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
                return;

            _isLoading = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
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

    public bool HasError =>
        !string.IsNullOrWhiteSpace(ErrorMessage);

    // =========================================================
    // LOAD
    // =========================================================

    public async Task LoadAsync()
    {
        await LoadPageAsync(1);
    }

    public async Task SearchAsync()
    {
        await LoadPageAsync(1);
    }

    public async Task NextPageAsync()
    {
        if (!CanGoNext)
            return;

        await LoadPageAsync(PageNumber + 1);
    }

    public async Task PreviousPageAsync()
    {
        if (!CanGoPrevious)
            return;

        await LoadPageAsync(PageNumber - 1);
    }

    public async Task ClearFiltersAsync()
    {
        try
        {
            _isResettingFilters = true;

            _debounceCts?.Cancel();

            SearchText = string.Empty;
            SelectedStatusOption = StatusOptions[0];
            FromDate = null;
            ToDate = null;
        }
        finally
        {
            _isResettingFilters = false;
        }

        await LoadPageAsync(1);
    }

    private async Task LoadPageAsync(int pageNumber)
    {
        // Any explicit filter/page change makes an outstanding
        // debounce irrelevant.
        _debounceCts?.Cancel();

        // Cancel the previous database request.
        _loadCts?.Cancel();
        _loadCts?.Dispose();

        _loadCts = new CancellationTokenSource();

        var token = _loadCts.Token;

        // Used to prevent an older request from updating the UI
        // after a newer request has started.
        var requestVersion =
            Interlocked.Increment(ref _requestVersion);

        try
        {
            IsLoading = true;
            SetError(null);

            if (FromDate.HasValue &&
                ToDate.HasValue &&
                FromDate.Value.Date > ToDate.Value.Date)
            {
                SetError(
                    "From date cannot be later than To date.");

                return;
            }

            var query = new SalesHistoryQuery
            {
                SearchText =
                    string.IsNullOrWhiteSpace(SearchText)
                        ? null
                        : SearchText.Trim(),

                Status =
                    _selectedStatus,

                FromDate =
                    FromDate?.Date,

                ToDate =
                    ToDate?.Date,

                PageNumber =
                    pageNumber,

                PageSize =
                    PageSize
            };

            var result =
                await _saleService.GetSalesHistoryAsync(
                    query,
                    token);

            // A newer request started while this one was running.
            if (requestVersion != _requestVersion)
                return;

            Sales.Clear();

            foreach (var sale in result.Items)
            {
                Sales.Add(sale);
            }

            PageNumber = result.PageNumber;
            TotalCount = result.TotalCount;
            TotalPages = result.TotalPages;
        }
        catch (OperationCanceledException)
        {
            // Normal behavior when a newer filter replaces this one.
        }
        catch (Exception ex)
        {
            // Don't let an obsolete request display an error.
            if (requestVersion != _requestVersion)
                return;

            Sales.Clear();

            PageNumber = 1;
            TotalCount = 0;
            TotalPages = 0;

            SetError(ex.Message);
        }
        finally
        {
            // Only the newest request owns the loading indicator.
            if (requestVersion == _requestVersion)
            {
                IsLoading = false;
            }
        }
    }

    private void SetError(string? message)
    {
        ErrorMessage = message;

        OnPropertyChanged(nameof(HasError));
    }

    // =========================================================
    // PROPERTY CHANGED
    // =========================================================

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    public async Task DebouncedSearchAsync()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();

        _debounceCts = new CancellationTokenSource();

        var token = _debounceCts.Token;

        try
        {
            await Task.Delay(700, token);

            if (token.IsCancellationRequested)
                return;

            await LoadPageAsync(1);
        }
        catch (OperationCanceledException)
        {
            // Expected when the user continues typing.
        }
    }
}

public sealed class SaleStatusOption
{
    public SaleStatusOption(
        SaleStatus? status,
        string displayName)
    {
        Status = status;
        DisplayName = displayName;
    }

    public SaleStatus? Status { get; }

    public string DisplayName { get; }

    public override string ToString() =>
        DisplayName;
}