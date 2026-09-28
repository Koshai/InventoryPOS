using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class PurchaseReturnHistoryViewModel : INotifyPropertyChanged
{
    private readonly IPurchaseReturnService _purchaseReturnService;

    private bool _isBusy;
    private string _searchText = string.Empty;
    private string? _errorMessage;

    public PurchaseReturnHistoryViewModel(
        IPurchaseReturnService purchaseReturnService)
    {
        _purchaseReturnService = purchaseReturnService;
    }

    public ObservableCollection<PurchaseReturnHistoryDto> Returns { get; }
        = new();

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
                return;

            _isBusy = value;
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
        !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasReturns =>
        Returns.Count > 0;

    public bool IsEmpty =>
        !IsBusy && Returns.Count == 0;

    public async Task LoadAsync()
    {
        await SearchAsync();
    }

    public async Task SearchAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var results =
                await _purchaseReturnService.GetHistoryAsync(
                    string.IsNullOrWhiteSpace(SearchText)
                        ? null
                        : SearchText.Trim());

            Returns.Clear();

            foreach (var item in results)
                Returns.Add(item);

            OnPropertyChanged(nameof(HasReturns));
            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;

            Returns.Clear();

            OnPropertyChanged(nameof(HasReturns));
            OnPropertyChanged(nameof(IsEmpty));
        }
        finally
        {
            IsBusy = false;

            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public void ClearSearch()
    {
        SearchText = string.Empty;
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