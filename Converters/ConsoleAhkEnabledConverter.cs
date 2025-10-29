using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Moodex.Services;

namespace Moodex.Converters
{
    public class ConsoleAhkEnabledConverter : IValueConverter
    {
        private static readonly JsonSettingsService _settings = new JsonSettingsService();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var consoleId = value as string;
            if (string.IsNullOrWhiteSpace(consoleId))
                return false;

            try
            {
                var cfg = _settings.Load();
                if (!cfg.IsAutoHotKeyInstalled)
                    return false;

                var enabled = cfg.AhkEnabledConsoleIds ?? new System.Collections.Generic.List<string>();
                return enabled.Any(id => string.Equals(id, consoleId, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}




