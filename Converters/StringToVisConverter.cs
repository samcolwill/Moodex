using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Moodex.Converters
{
    public sealed class StringToVisConverter : IValueConverter
    {
        public object Convert(object value, Type _, object parameter, CultureInfo __)
        {
            if (value is string str && parameter is string target)
            {
                return str.Equals(target, StringComparison.OrdinalIgnoreCase) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


