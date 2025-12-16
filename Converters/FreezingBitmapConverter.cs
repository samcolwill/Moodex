using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Moodex.Converters
{
    [ValueConversion(typeof(string), typeof(ImageSource))]
    public class FreezingBitmapConverter : IValueConverter
    {
        // thread-safe cache in case you ever load in parallel
        private static readonly ConcurrentDictionary<string, ImageSource> _cache
            = new();
        // Decode covers to this width (in device pixels) to reduce memory and improve quality
        private const int DecodeWidthPx = 300;

        public static void Invalidate(string path)
        {
            try
            {
                // remove any entries matching this path (with any timestamp suffix)
                foreach (var key in _cache.Keys)
                {
                    if (key.StartsWith(path + "|", StringComparison.OrdinalIgnoreCase))
                    {
                        _cache.TryRemove(key, out _);
                    }
                }
            }
            catch { }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            if (string.IsNullOrWhiteSpace(path))
                return DependencyProperty.UnsetValue;

            // don't try to load if the file isn't there
            if (!File.Exists(path))
                return DependencyProperty.UnsetValue;

            // Use last write time and decode width in cache key so updates bust the cache automatically
            string key;
            try
            {
                var lastWrite = File.GetLastWriteTimeUtc(path).Ticks;
                key = $"{path}|w{DecodeWidthPx}|{lastWrite}";
            }
            catch
            {
                key = $"{path}|w{DecodeWidthPx}|0";
            }

            if (_cache.TryGetValue(key, out var cached))
                return cached;

            try
            {
                var bmp = new BitmapImage();

                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                // Downscale at decode time to reduce memory usage
                bmp.DecodePixelWidth = Math.Max(1, DecodeWidthPx);
                bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.IgnoreImageCache;
                bmp.EndInit();
                bmp.Freeze();  // allow cross-thread and reuse

                _cache[key] = bmp;
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

