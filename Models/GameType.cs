using System.Text.Json.Serialization;

namespace SamsGameLauncher.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GameType
    {
        Emulated,    // Uses an external emulator to run a ROM
        Native,      // Launches a native executable directly
        FolderBased  // Points at a folder of files, launched via emulator
    }
}
