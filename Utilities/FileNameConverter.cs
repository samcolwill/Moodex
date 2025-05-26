// File: Utilities/FileNameConverter.cs
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace SamsGameLauncher.Utilities
{
    public class FileNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return Path.GetFileName(path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
