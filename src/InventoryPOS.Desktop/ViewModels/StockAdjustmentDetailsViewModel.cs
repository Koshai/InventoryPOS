using InventoryPOS.Application.DTOs.Inventory;
using InventoryPOS.Application.Interfaces;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InventoryPOS.Desktop.ViewModels;

public class StockAdjustmentDetailsViewModel : INotifyPropertyChanged
{
    private readonly IStockAdjustmentService _stockAdjustmentService;

    private StockAdjustmentDetailsDto? _adjustment;
    private bool _isLoading;
    private string? _errorMessage;

    public StockAdjustmentDetailsDto? Adjustment
    {
        get => _adjustment;

        private set
        {
            if (_adjustment == value)
                return;

            _adjustment = value;

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
        }
    }

    public StockAdjustmentDetailsViewModel(
        IStockAdjustmentService stockAdjustmentService)
    {
        _stockAdjustmentService =
            stockAdjustmentService;
    }

    public async Task LoadAsync(Guid adjustmentId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            Adjustment =
                await _stockAdjustmentService
                    .GetByIdAsync(adjustmentId);

            if (Adjustment is null)
            {
                ErrorMessage =
                    "Stock adjustment could not be found.";
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