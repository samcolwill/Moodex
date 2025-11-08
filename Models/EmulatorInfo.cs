using Moodex.Utilities;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Moodex.Models
{
    public class EmulatorInfo
    {
        // Emulator data loaded/saved to emulators.json
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Guid { get; set; }
        public List<string> EmulatedConsoleIds { get; set; } = new List<string>();
        public string ExecutablePath { get; set; } = string.Empty;
        public string DefaultArguments { get; set; } = string.Empty;

        // Runtime-only helpers
        public string? EmulatedConsoleNames
            => EmulatedConsoleIds == null || EmulatedConsoleIds.Count == 0
               ? null
               : string.Join(", ",
                   EmulatedConsoleIds
                       .Select(ConsoleRegistry.GetDisplayName)
                       .Where(n => !string.IsNullOrWhiteSpace(n)));
    }
}
