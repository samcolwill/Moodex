using System.IO;
using System.Text.Json.Serialization;
using SamsGameLauncher.Utilities;

namespace SamsGameLauncher.Models
{
    public class FolderBasedGame : GameBase
    {
        // Matches an entry in GameLibrary.Emulators
        public string EmulatorId { get; set; } = string.Empty;

        // Full path to the game’s folder on disk
        public string FolderPath { get; set; } = string.Empty;

        // Identifies this as the folder‐based subtype
        public override GameType GameType => GameType.FolderBased;

        // At runtime, looks for an image named after the folder
        [JsonIgnore]
        public override string GameCoverUri
        {
            get
            {
                if (string.IsNullOrEmpty(FolderPath))
                    return string.Empty;

                var folderName = new DirectoryInfo(FolderPath).Name;
                var coverPath = GameCoverLocator.FindGameCover(FolderPath, folderName);

                if (string.IsNullOrEmpty(coverPath))
                    return string.Empty;

                return new Uri(coverPath).AbsoluteUri;
            }
        }

        // Runtime‐only: populated in GameLibrary or ViewModel
        [JsonIgnore]
        public Emulator? Emulator { get; set; }
    }
}