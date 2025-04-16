using System.Text.Json.Serialization;

namespace SamsGameLauncher.Models
{
    // Store the enum as its string name in JSON (e.g. "Emulated" rather than 0)
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GameType
    {
        Emulated,    // Uses an external emulator to run a ROM
        Native,      // Launches a native executable directly
        FolderBased  // Points at a folder of files, launched via emulator
    }
}
