using System.Globalization;
using Avalonia.Data.Converters;

namespace ChatApp.Avalonia.Converters
{
    public static class AppConverters
    {
        public static readonly IValueConverter InverseBool = new InverseBoolConverter();
    }

    internal class InverseBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return value;
        }
    }
}
