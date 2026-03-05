using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MdModManager.Converters;

/// <summary>Converts a font family name string to an Avalonia FontFamily for use in item templates.</summary>
public class StringToFontFamilyConverter : IValueConverter
{
    public static readonly StringToFontFamilyConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string name && !string.IsNullOrWhiteSpace(name))
        {
            try
            {
                return FontFamily.Parse(name);
            }
            catch
            {
                // fall through
            }
        }
        return FontFamily.Default;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
