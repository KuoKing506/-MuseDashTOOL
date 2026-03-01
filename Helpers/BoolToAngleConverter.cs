using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MdModManager.Helpers;

public class BoolToAngleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? 180.0 : 0.0;
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
