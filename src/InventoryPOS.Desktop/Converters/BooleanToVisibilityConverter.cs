using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace InventoryPOS.Desktop.Converters;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        string language)
    {
        return value is bool isVisible && isVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        string language)
    {
        throw new NotSupportedException();
    }
}