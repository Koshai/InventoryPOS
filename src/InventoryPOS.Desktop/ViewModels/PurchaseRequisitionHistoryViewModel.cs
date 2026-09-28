using System;
using InventoryPOS.Application.DTOs.Purchasing;
using InventoryPOS.Application.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class PurchaseRequisitionHistoryViewModel
    : INotifyPropertyChanged
{
    private readonly IPurchaseRequisitionService
        _purchaseRequisitionService;

    private string? _searchText;
    private bool _isLoading;
    private string? _errorMessage;

    public PurchaseRequisitionHistoryViewModel(
        IPurchaseRequisitionService purchaseRequisitionService)
    {
        _purchaseRequisitionService =
            purchaseRequisitionService;
    }

    public ObservableCollection<PurchaseRequisitionHistoryDto>
        Requisitions
    { get; } = new();

    public string? SearchText
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
        !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasNoResults =>
        !IsLoading && Requisitions.Count == 0;

    public async Task LoadAsync()
    {
        await LoadInternalAsync(SearchText);
    }

    public async Task SearchAsync()
    {
        await LoadInternalAsync(SearchText);
    }

    public async Task ClearSearchAsync()
    {
        SearchText = null;

        await LoadInternalAsync(null);
    }

    private async Task LoadInternalAsync(
        string? search)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var results =
                await _purchaseRequisitionService
                    .GetHistoryAsync(search);

            Requisitions.Clear();

            foreach (var item in results)
                Requisitions.Add(item);

            OnPropertyChanged(nameof(HasNoResults));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;

            Requisitions.Clear();

            OnPropertyChanged(nameof(HasNoResults));
        }
        finally
        {
            IsLoading = false;

            OnPropertyChanged(nameof(HasNoResults));
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
            new PropertyChangedEventArgs(propertyName));
    }
}