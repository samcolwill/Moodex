using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SamsGameLauncher.Converters
{
    [ValueConversion(typeof(string), typeof(BitmapImage))]
    public class FreezingBitmapConverter : IValueConverter
    {
        // value is the file-path of your image
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            Debug.WriteLine($"[FreezingBitmap] got path='{path}'");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;  // ← read file fully, then release
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache | BitmapCreateOptions.IgnoreColorProfile;
            bmp.EndInit();
            bmp.Freeze();                                  // ← make it thread‑safe & immutable

            return bmp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
