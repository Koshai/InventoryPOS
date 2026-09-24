using InventoryPOS.Application.Interfaces;
using InventoryPOS.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class ProductDetailsPage : Page
{
    private Guid? _productId;
    private readonly IProductImageService _productImageService;

    public ProductDetailsViewModel ViewModel { get; }

    public ProductDetailsPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<ProductDetailsViewModel>();

        _productImageService =
            App.Services.GetRequiredService<IProductImageService>();

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
}