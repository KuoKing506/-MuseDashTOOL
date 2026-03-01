using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MdModManager.Helpers;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? Brushes.White : new SolidColorBrush(Color.Parse("#707070"));
        }
        return new SolidColorBrush(Color.Parse("#707070"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
