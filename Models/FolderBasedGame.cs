using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;
using SamsGameLauncher.Utilities;

namespace SamsGameLauncher.Models
{
    public class FolderBasedGame : GameBase
    {
        public string EmulatorId { get; set; }
        public string FolderPath { get; set; } // Path to the game folder.
        public override GameType GameType => GameType.FolderBased;

        [JsonIgnore]
        public override string GameCoverUri
        {
            get
            {
                string folderName = new DirectoryInfo(FolderPath).Name;

                var coverPath = GameCoverLocator.FindGameCover(FolderPath, folderName);
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
