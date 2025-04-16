using System.IO;
using System.Text.Json.Serialization;
using SamsGameLauncher.Utilities;

namespace SamsGameLauncher.Models
{
    public class EmulatedGame : GameBase
    {
        // Matches an entry in GameLibrary.Emulators
        public string EmulatorId { get; set; } = string.Empty;

        // Full path to the ROM file on disk
        public string GamePath { get; set; } = string.Empty;

        // Identifies this as an emulated‐type game
        public override GameType GameType => GameType.Emulated;

        // At runtime, looks for an image next to the ROM named like "{BaseName}.png/.jpg/etc."
        // Returns a properly escaped file:// URI or empty string if none found
        [JsonIgnore]
        public override string GameCoverUri
        {
            get
            {
                // Guard against missing path
                var directory = Path.GetDirectoryName(GamePath);
                if (string.IsNullOrEmpty(directory))
                    return string.Empty;

                var baseName = Path.GetFileNameWithoutExtension(GamePath);
                var coverPath = GameCoverLocator.FindGameCover(directory, baseName);

                if (string.IsNullOrEmpty(coverPath))
                    return string.Empty;

                // Use Uri class to handle proper escaping
                return new Uri(coverPath).AbsoluteUri;
            }
        }

        // Runtime‐only: the Emulator instance, wired up in the ViewModel or GameLibrary
        [JsonIgnore]
        public Emulator? Emulator { get; set; }
    }
}