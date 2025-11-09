using System.Collections.Generic;
using System.Linq;
using Moodex.Models;
using Moodex.Services;

namespace Moodex.Utilities
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

        public static (double w, double h)? GetCoverAspect(string consoleId)
        {
            var c = _consoles.FirstOrDefault(x => x.Id == consoleId);
            if (c == null || c.CoverAspectW <= 0 || c.CoverAspectH <= 0) return null;
            return (c.CoverAspectW, c.CoverAspectH);
        }
    }
}
