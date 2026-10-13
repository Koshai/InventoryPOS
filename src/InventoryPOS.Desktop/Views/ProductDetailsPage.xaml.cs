using System.IO;
using InventoryPOS.Application.DTOs.Products;
using InventoryPOS.Application.Interfaces;
using InventoryPOS.Desktop.ViewModels;
using InventoryPOS.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using Windows.Storage;
using Windows.System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class ProductDetailsPage : Page
{
    private Guid? _productId;
    private readonly IProductImageService _productImageService;
    private readonly IProductBarcodeLabelService _productBarcodeLabelService;

    public ProductDetailsViewModel ViewModel { get; }

    public ProductDetailsPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<
                ProductDetailsViewModel>();

        _productImageService =
            App.Services.GetRequiredService<
                IProductImageService>();

        _productBarcodeLabelService =
            App.Services.GetRequiredService<
                IProductBarcodeLabelService>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Guid productId)
        {
            _productId = productId;
        }

        if (_productId.HasValue)
        {
            await ViewModel.LoadAsync(
                _productId.Value);
            
            var imagePath = await _productImageService.GetPrimaryImagePathAsync(_productId.Value);

            if (!string.IsNullOrWhiteSpace(
                imagePath))
            {
                ProductImage.Source =
                    new BitmapImage(
                        new Uri(imagePath));
            }
            else
            {
                ProductImage.Source = null;
            }
        }
    }

    private void BackButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void EditProductButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ViewModel.Product is null)
            return;

        Frame.Navigate(
            typeof(EditProductPage),
            ViewModel.Product.Id);
    }

    private async void ManageBarcodesButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not ProductVariantDto variant)
        {
            return;
        }

        BarcodeValueTextBox.Text =
            string.Empty;

        BarcodeTypeComboBox.SelectedIndex =
            0;

        PrimaryBarcodeCheckBox.IsChecked =
            false;

        LabelCopiesNumberBox.Value =
    1;

        LabelShowPriceCheckBox.IsChecked =
            true;

        LabelShowSkuCheckBox.IsChecked =
            true;

        BarcodeErrorInfoBar.IsOpen =
            false;

        await ViewModel.LoadBarcodesAsync(
            variant.Id,
            variant.SKU);

        BarcodeDialog.XamlRoot =
            XamlRoot;

        await BarcodeDialog.ShowAsync();
    }

    private async void BarcodeDialog_PrimaryButtonClick(
    ContentDialog sender,
    ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;

        BarcodeErrorInfoBar.IsOpen =
            false;

        var value =
            BarcodeValueTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            BarcodeErrorInfoBar.Message =
                "Enter or scan a barcode.";

            BarcodeErrorInfoBar.IsOpen =
                true;

            return;
        }

        if (BarcodeTypeComboBox.SelectedItem
                is not ComboBoxItem selectedItem ||
            selectedItem.Tag is not string typeName ||
            !Enum.TryParse<BarcodeType>(
                typeName,
                out var barcodeType))
        {
            BarcodeErrorInfoBar.Message =
                "Select a valid barcode type.";

            BarcodeErrorInfoBar.IsOpen =
                true;

            return;
        }

        try
        {
            await ViewModel.AddBarcodeAsync(
                value,
                barcodeType,
                PrimaryBarcodeCheckBox.IsChecked == true);

            BarcodeValueTextBox.Text =
                string.Empty;

            PrimaryBarcodeCheckBox.IsChecked =
                false;

            BarcodeValueTextBox.Focus(
                FocusState.Programmatic);
        }
        catch (Exception ex)
        {
            BarcodeErrorInfoBar.Message =
                ex.Message;

            BarcodeErrorInfoBar.IsOpen =
                true;
        }
    }

    private async void RemoveBarcodeButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not ProductBarcodeDto barcode)
        {
            return;
        }

        try
        {
            BarcodeErrorInfoBar.IsOpen =
                false;

            await ViewModel.DeleteBarcodeAsync(
                barcode.Id);
        }
        catch (Exception ex)
        {
            BarcodeErrorInfoBar.Message =
                ex.Message;

            BarcodeErrorInfoBar.IsOpen =
                true;
        }
    }

    private async void PrintBarcodeLabelButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not ProductBarcodeDto barcode)
        {
            return;
        }

        if (!ViewModel.SelectedVariantId.HasValue)
        {
            BarcodeErrorInfoBar.Message =
                "No product variant is selected.";

            BarcodeErrorInfoBar.IsOpen =
                true;

            return;
        }

        BarcodeErrorInfoBar.IsOpen =
            false;

        try
        {
            var copies =
                double.IsNaN(LabelCopiesNumberBox.Value)
                    ? 1
                    : Convert.ToInt32(
                        LabelCopiesNumberBox.Value);

            var pdfBytes =
                await _productBarcodeLabelService
                    .GenerateLabelPdfAsync(
                        new ProductBarcodeLabelRequest
                        {
                            ProductVariantId =
                                ViewModel.SelectedVariantId.Value,

                            ProductBarcodeId =
                                barcode.Id,

                            Copies =
                                copies,

                            ShowPrice =
                                LabelShowPriceCheckBox.IsChecked == true,

                            ShowSku =
                                LabelShowSkuCheckBox.IsChecked == true
                        });

            var tempFolder =
                ApplicationData.Current
                    .TemporaryFolder;

            var safeBarcode =
                MakeSafeFileName(
                    barcode.Value);

            var file =
                await tempFolder.CreateFileAsync(
                    $"Barcode-{safeBarcode}.pdf",
                    CreationCollisionOption.GenerateUniqueName);

            await FileIO.WriteBytesAsync(
                file,
                pdfBytes);

            var launched =
                await Launcher.LaunchFileAsync(
                    file);

            if (!launched)
            {
                throw new InvalidOperationException(
                    "The barcode label PDF was generated, " +
                    "but Windows could not open it.");
            }
        }
        catch (Exception ex)
        {
            BarcodeErrorInfoBar.Message =
                ex.Message;

            BarcodeErrorInfoBar.IsOpen =
                true;
        }
    }

    private static string MakeSafeFileName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "label";
        }

        var invalidCharacters = System.IO.Path.GetInvalidFileNameChars();

        var safe =
            new string(
                value
                    .Where(
                        c =>
                            !invalidCharacters
                                .Contains(c))
                    .ToArray());

        return string.IsNullOrWhiteSpace(safe)
            ? "label"
            : safe;
    }
}