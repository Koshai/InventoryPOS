using InventoryPOS.Application.DTOs.SalesReturns;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Domain.Enums;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public sealed class SalesReturnsViewModel : INotifyPropertyChanged
{
    private readonly ISalesReturnService _salesReturnService;

    private CancellationTokenSource? _searchCancellation;

    private string _searchText = string.Empty;
    private bool _isLoading;
    private string? _errorMessage;

    private int _pageNumber = 1;
    private const int PageSize = 25;

    private int _totalCount;
    private int _totalPages;

    public SalesReturnsViewModel(
        ISalesReturnService salesReturnService)
    {
        _salesReturnService =
            salesReturnService;
    }

    public ObservableCollection<SalesReturnHistoryItemDto>
        Items
    { get; } = new();

    public string SearchText
    {
        get => _searchText;

        set
        {
            if (_searchText == value)
                return;

            _searchText = value;

            OnPropertyChanged();

            _ = DebouncedSearchAsync();
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
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError =>
        !string.IsNullOrWhiteSpace(
            ErrorMessage);

    public int PageNumber
    {
        get => _pageNumber;

        private set
        {
            if (_pageNumber == value)
                return;

            _pageNumber = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(PageText));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
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
            OnPropertyChanged(nameof(ResultText));
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
            OnPropertyChanged(nameof(PageText));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
        }
    }

    public string ResultText =>
        TotalCount == 1
            ? "1 return"
            : $"{TotalCount:N0} returns";

    public string PageText =>
        TotalPages <= 0
            ? "Page 1 of 1"
            : $"Page {PageNumber} of {TotalPages}";

    public bool CanGoPrevious =>
        !IsLoading &&
        PageNumber > 1;

    public bool CanGoNext =>
        !IsLoading &&
        TotalPages > 0 &&
        PageNumber < TotalPages;

    public async Task LoadAsync()
    {
        PageNumber = 1;

        await LoadPageAsync();
    }

    public async Task RefreshAsync()
    {
        await LoadPageAsync();
    }

    public async Task NextPageAsync()
    {
        if (!CanGoNext)
            return;

        PageNumber++;

        await LoadPageAsync();
    }

    public async Task PreviousPageAsync()
    {
        if (!CanGoPrevious)
            return;

        PageNumber--;

        await LoadPageAsync();
    }

    private async Task DebouncedSearchAsync()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();

        _searchCancellation =
            new CancellationTokenSource();

        var token =
            _searchCancellation.Token;

        try
        {
            await Task.Delay(
                700,
                token);

            PageNumber = 1;

            await LoadPageAsync(
                token);
        }
        catch (OperationCanceledException)
        {
            // Expected while the user is still typing.
        }
    }

    private async Task LoadPageAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var result =
                await _salesReturnService
                    .GetReturnHistoryAsync(
                        new SalesReturnHistoryQueryDto
                        {
                            SearchText =
                                string.IsNullOrWhiteSpace(
                                    SearchText)
                                    ? null
                                    : SearchText.Trim(),

                            PageNumber =
                                PageNumber,

                            PageSize =
                                PageSize
                        },
                        cancellationToken);

            Items.Clear();

            foreach (var item
                     in result.Items)
            {
                Items.Add(item);
            }

            TotalCount =
                result.TotalCount;

            TotalPages =
                result.TotalPages;

            if (TotalPages > 0 &&
                PageNumber > TotalPages)
            {
                PageNumber =
                    TotalPages;

                await LoadPageAsync(
                    cancellationToken);

                return;
            }

            OnPropertyChanged(
                nameof(CanGoPrevious));

            OnPropertyChanged(
                nameof(CanGoNext));
        }
        catch (OperationCanceledException)
        {
            // Search was superseded.
        }
        catch (Exception ex)
        {
            ErrorMessage =
                ex.Message;
        }
        finally
        {
            IsLoading = false;

            OnPropertyChanged(
                nameof(CanGoPrevious));

            OnPropertyChanged(
                nameof(CanGoNext));
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