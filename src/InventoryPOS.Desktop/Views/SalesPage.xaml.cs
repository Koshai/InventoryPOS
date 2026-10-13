using System;
using InventoryPOS.Application.DTOs.Sales;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace InventoryPOS.Desktop.Views;

public sealed partial class SalesPage : Page
{
    public SalesViewModel ViewModel { get; }

    private bool _hasLoaded;
    private bool _suppressFilterEvents;

    public SalesPage()
    {
        InitializeComponent();

        NavigationCacheMode =
            NavigationCacheMode.Required;

        ViewModel =
            App.Services.GetRequiredService<SalesViewModel>();

        DataContext = ViewModel;
    }

    // =========================================================
    // NAVIGATION
    // =========================================================

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // If this page has already been initialized, we are most
        // likely returning from InvoicePage through Frame.GoBack().
        //
        // Keep:
        // - search text
        // - status
        // - dates
        // - page number
        // - existing result collection
        //
        // Do NOT query the database again.
        if (_hasLoaded)
            return;

        _hasLoaded = true;
        _suppressFilterEvents = true;

        try
        {
            ConfigureDatePickers();

            await ViewModel.LoadAsync();
        }
        finally
        {
            _suppressFilterEvents = false;
        }
    }

    private void ConfigureDatePickers()
    {
        var today = DateTimeOffset.Now;

        var minimumDate =
            new DateTimeOffset(
                2020,
                1,
                1,
                0,
                0,
                0,
                today.Offset);

        var maximumDate =
            today.AddYears(1);

        FromDatePicker.MinDate = minimumDate;
        ToDatePicker.MinDate = minimumDate;

        FromDatePicker.MaxDate = maximumDate;
        ToDatePicker.MaxDate = maximumDate;
    }

    // =========================================================
    // SEARCH
    // =========================================================

    private async void SearchButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.SearchAsync();
    }

    private async void SearchTextBox_KeyDown(
        object sender,
        KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.Enter)
            return;

        e.Handled = true;

        await ViewModel.SearchAsync();
    }

    private async void SearchTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (ShouldIgnoreFilterEvent())
            return;

        await ViewModel.DebouncedSearchAsync();
    }

    // =========================================================
    // FILTERS
    // =========================================================

    private async void StatusComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (ShouldIgnoreFilterEvent())
            return;

        await ViewModel.SearchAsync();
    }

    private async void FilterDateChanged(
        CalendarDatePicker sender,
        CalendarDatePickerDateChangedEventArgs args)
    {
        if (ShouldIgnoreFilterEvent())
            return;

        await ViewModel.SearchAsync();
    }

    private bool ShouldIgnoreFilterEvent()
    {
        return
            !IsLoaded ||
            _suppressFilterEvents ||
            ViewModel.IsResettingFilters;
    }

    // =========================================================
    // CLEAR / REFRESH
    // =========================================================

    private async void ClearButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.ClearFiltersAsync();
    }

    private async void RefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.SearchAsync();
    }

    // =========================================================
    // PAGINATION
    // =========================================================

    private async void PreviousButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.PreviousPageAsync();
    }

    private async void NextButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ViewModel.NextPageAsync();
    }

    // =========================================================
    // INVOICE
    // =========================================================

    private void ViewSaleButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not SalesHistoryDto sale)
        {
            return;
        }

        Frame.Navigate(
            typeof(InvoicePage),
            sale.SaleId);
    }

    private void ReturnsButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        Frame.Navigate(
            typeof(SalesReturnsPage));
    }

    private void PricingButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not SalesHistoryDto sale)
        {
            return;
        }

        sale.IsPricingExpanded = !sale.IsPricingExpanded;

        // Refresh this ListView item so x:Bind reflects
        // the updated expansion state.
        var index = ViewModel.Sales.IndexOf(sale);

        if (index < 0)
            return;

        ViewModel.Sales.RemoveAt(index);
        ViewModel.Sales.Insert(index, sale);
    }
}