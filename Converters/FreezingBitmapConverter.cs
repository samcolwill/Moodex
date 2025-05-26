using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SamsGameLauncher.Converters
{
    [ValueConversion(typeof(string), typeof(ImageSource))]
    public class FreezingBitmapConverter : IValueConverter
    {
        // thread-safe cache in case you ever load in parallel
        private static readonly ConcurrentDictionary<string, ImageSource> _cache
            = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            if (string.IsNullOrWhiteSpace(path))
                return DependencyProperty.UnsetValue;

            // don't try to load if the file isn't there
            if (!File.Exists(path))
                return DependencyProperty.UnsetValue;

            // return cached instance if we already did this one
            if (_cache.TryGetValue(path, out var cached))
                return cached;

            try
            {
                var bmp = new BitmapImage();

                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bmp.EndInit();
                bmp.Freeze();  // allow cross-thread and reuse

                _cache[path] = bmp;
                return bmp;
            }
            catch (Exception ex)
            {
                // swallow and log, then return nothing so UI doesn't die
                System.Diagnostics.Debug.WriteLine($"FreezingBitmapConverter: failed to load '{path}': {ex}");
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // we only ever go one-way
            throw new NotSupportedException();
        }
    }
}
