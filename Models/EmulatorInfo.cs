using SamsGameLauncher.Utilities;
using System.Text.Json.Serialization;

namespace SamsGameLauncher.Models
{
    public class EmulatorInfo
    {
        // Emulator data loaded/saved to emulators.json
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string EmulatedConsoleId { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string DefaultArguments { get; set; } = string.Empty;

        // Runtime-only helpers
        [JsonIgnore]
        public string? EmulatedConsoleName => Utilities.ConsoleRegistry.GetDisplayName(EmulatedConsoleId);
    }
}