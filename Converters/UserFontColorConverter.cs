using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MdModManager.Converters;

/// <summary>
/// Returns a green brush for user-installed fonts, or UnsetValue (inherit) for system fonts.
/// </summary>
public class UserFontColorConverter : IValueConverter
{
    public static readonly UserFontColorConverter Instance = new();

    // Shared reference — SettingsViewModel writes to this when refreshing font list
    public static HashSet<string> UserFontNames { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string name && UserFontNames.Contains(name))
            return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)); // Material Green 500

        // Return UnsetValue so Avalonia falls back to the inherited parent foreground
        return AvaloniaProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
