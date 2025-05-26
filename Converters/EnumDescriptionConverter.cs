using System;
using System.Globalization;
using System.Windows.Data;
using SamsGameLauncher.Models;

namespace SamsGameLauncher.Converters
{
    public class EnumDescriptionConverter : IValueConverter
    {
        // Convert enum → description
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is Enum e)
                ? e.GetDescription()
                : value?.ToString() ?? "";

        // Not used in one-way bindings
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
