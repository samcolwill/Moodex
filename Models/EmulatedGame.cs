using System;
using System.IO;
using System.Text.Json.Serialization;
using SamsGameLauncher.Utilities;

namespace SamsGameLauncher.Models
{
    public class EmulatedGame : GameBase
    {
        public string EmulatorId { get; set; }
        public string GamePath { get; set; } // Path to the ROM file.
        public override GameType GameType => GameType.Emulated;

        // Compute the cover image URI by checking for valid image extensions.
        [JsonIgnore]
        public override string GameCoverUri
        {
            get
            {
                var directory = Path.GetDirectoryName(GamePath);
                var baseFileName = Path.GetFileNameWithoutExtension(GamePath);
                var coverPath = GameCoverLocator.FindGameCover(directory, baseFileName);
                if (coverPath != null)
                {
                    return $"file:///{coverPath.Replace("\\", "/").Replace(" ", "%20")}";
                }
                return null;
            }
        }

        // A runtime property; not persisted.
        [JsonIgnore]
        public Emulator Emulator { get; set; }
    }
}