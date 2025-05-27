using System.Collections.Generic;
using System.Linq;
using SamsGameLauncher.Models;
using SamsGameLauncher.Services;

namespace SamsGameLauncher.Utilities
{
    public static class ConsoleRegistry
    {
        private static IReadOnlyList<ConsoleInfo> _consoles = new List<ConsoleInfo>();

        public static void Refresh(ISettingsService settingsService)
        {
            var cfg = settingsService.Load();
            _consoles = cfg.Consoles;
        }

        public static string? GetDisplayName(string consoleId)
            => _consoles.FirstOrDefault(c => c.Id == consoleId) ?.Name;
    }
}