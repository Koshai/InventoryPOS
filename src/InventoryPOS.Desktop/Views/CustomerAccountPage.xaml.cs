using InventoryPOS.Application.DTOs.Customers;
using InventoryPOS.Desktop.ViewModels;
using InventoryPOS.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace InventoryPOS.Desktop.Views;

public sealed partial class CustomerAccountPage : Page
{
    public CustomerAccountViewModel ViewModel { get; }

    public CustomerAccountPage()
    {
        InitializeComponent();

        ViewModel =
            App.Services.GetRequiredService<
                CustomerAccountViewModel>();

        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(
        NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Guid customerId)
        {
            await ViewModel.LoadAsync(customerId);
            if (ViewModel.HasPendingPayment)
            {
                await ShowRecoveryDialogAsync();
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
            return;
        }

        Frame.Navigate(
            typeof(CustomersPage));
    }

    private async void ReceiveAccountPaymentButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!ViewModel.CanStartPayment)
        {
            await ShowRecoveryDialogAsync();
            return;
        }

        ViewModel.PrepareAccountPayment();

        await ShowPaymentDialogAsync(
            "Receive Account Payment",
            "Payment will be applied to the oldest outstanding invoices first.");
    }

    private async void InvoicePaymentButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!ViewModel.CanStartPayment)
        {
            await ShowRecoveryDialogAsync();
            return;
        }

        if (sender is not Button button ||
            button.Tag is not CustomerOutstandingSaleDto invoice)
        {
            return;
        }

        ViewModel.PrepareInvoicePayment(invoice);

        await ShowPaymentDialogAsync(
            $"Receive Payment — {invoice.InvoiceNumber}",
            $"Outstanding: ৳{invoice.DueAmount:N2}");
    }

    private async System.Threading.Tasks.Task
        ShowPaymentDialogAsync(
            string title,
            string subtitle)
    {
        var amountBox = new NumberBox
        {
            Header = "Amount",
            Minimum = 0,
            SpinButtonPlacementMode =
                NumberBoxSpinButtonPlacementMode.Hidden,
            Value = ViewModel.PaymentAmount
        };

        var methodBox = new ComboBox
        {
            Header = "Payment Method",
            ItemsSource = ViewModel.PaymentMethods,
            SelectedItem =
                ViewModel.SelectedPaymentMethod,
            HorizontalAlignment =
                HorizontalAlignment.Stretch
        };

        var referenceBox = new TextBox
        {
            Header = ViewModel.ReferenceLabel,
            Text =
                ViewModel.ReferenceNumber ?? string.Empty,
            PlaceholderText =
                "Enter payment reference"
        };

        var notesBox = new TextBox
        {
            Header = "Notes",
            Text =
                ViewModel.PaymentNotes ?? string.Empty,
            PlaceholderText =
                "Optional notes",
            AcceptsReturn = true,
            TextWrapping =
                TextWrapping.Wrap,
            MinHeight = 70
        };

        void UpdateReferenceState()
        {
            if (methodBox.SelectedItem
                is PaymentMethod method)
            {
                ViewModel.SelectedPaymentMethod =
                    method;
            }

            referenceBox.Header =
                ViewModel.ReferenceLabel;

            referenceBox.IsEnabled =
                ViewModel.RequiresReference;

            if (!ViewModel.RequiresReference)
            {
                referenceBox.Text =
                    string.Empty;
            }
        }

        methodBox.SelectionChanged +=
            (_, _) => UpdateReferenceState();

        UpdateReferenceState();

        var content =
            new StackPanel
            {
                Spacing = 12
            };

        content.Children.Add(
            new TextBlock
            {
                Text = subtitle,
                Opacity = 0.7,
                TextWrapping =
                    TextWrapping.Wrap
            });

        content.Children.Add(amountBox);
        content.Children.Add(methodBox);
        content.Children.Add(referenceBox);
        content.Children.Add(notesBox);

        var dialog =
            new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = title,
                Content = content,
                PrimaryButtonText =
                    "Receive Payment",
                CloseButtonText =
                    "Cancel",
                DefaultButton =
                    ContentDialogButton.Primary
            };

        var dialogResult =
            await dialog.ShowAsync();

        if (dialogResult !=
            ContentDialogResult.Primary)
        {
            ViewModel.CancelPayment();
            return;
        }

        ViewModel.PaymentAmount =
            amountBox.Value;

        if (methodBox.SelectedItem
            is PaymentMethod method)
        {
            ViewModel.SelectedPaymentMethod =
                method;
        }

        ViewModel.ReferenceNumber =
            referenceBox.Text;

        ViewModel.PaymentNotes =
            notesBox.Text;

        if (ViewModel.HasSelectedInvoice)
        {
            await ViewModel
                .ReceiveInvoicePaymentAsync();
        }
        else
        {
            await ViewModel
                .ReceiveAccountPaymentAsync();
        }
    }

    private async System.Threading.Tasks.Task ShowRecoveryDialogAsync()
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Pending Customer Payment",
            Content =
                "A previous payment has an unresolved outcome. " +
                "Retry the original payment request before recording another.",
            PrimaryButtonText = "Retry Pending Payment",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.RetryPendingPaymentAsync();
        }
    }
}