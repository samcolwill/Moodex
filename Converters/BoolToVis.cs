using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SamsGameLauncher.Converters
{
    public sealed class BoolToVis : IValueConverter
    {
        public object Convert(object value, Type _, object parameter, CultureInfo __)
        {
            bool val = (bool)value;
            if (parameter is string invert && invert.Equals("Invert", StringComparison.OrdinalIgnoreCase))
                val = !val;
            return val ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}