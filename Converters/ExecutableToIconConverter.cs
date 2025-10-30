// File: Utilities/ExecutableToIconConverter.cs
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows;
using System.Windows.Interop;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Moodex.Converters
{
    public class ExecutableToIconConverter : IValueConverter
    {
        public int TargetSize { get; set; } = 32;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return DependencyProperty.UnsetValue;

            try
            {
                using var icon = Icon.ExtractAssociatedIcon(path);
                if (icon == null) return DependencyProperty.UnsetValue;

                using var bmp = icon.ToBitmap();
                var hBitmap = bmp.GetHbitmap();

                var img = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    nint.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(TargetSize, TargetSize)); // Fixed issue here

                NativeMethods.DeleteObject(hBitmap);
                return img;
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(nint hObject);
    }
}

