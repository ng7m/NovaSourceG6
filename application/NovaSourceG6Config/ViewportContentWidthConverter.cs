using System.Globalization;
using System.Windows.Data;

namespace NovaSourceG6Config;

public sealed class ViewportContentWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        // Account for the main grid's margins and preserve scrolling at very small widths.
        value is double width && double.IsFinite(width) ? Math.Max(460, width - 32) : 460d;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
