using System;
using System.IO;
using System.Text.Json.Serialization;
using SamsGameLauncher.Utilities;

namespace SamsGameLauncher.Models
{
    public class NativeGame : GameBase
    {
        public string ExePath { get; set; } // Path to the executable.
        public override GameType GameType => GameType.Native;

        [JsonIgnore]
        public override string GameCoverUri
        {
            get
            {
                var directory = Path.GetDirectoryName(ExePath);
                var baseFileName = Path.GetFileNameWithoutExtension(ExePath);
                var coverPath = GameCoverLocator.FindGameCover(directory, baseFileName);
                if (coverPath != null)
                {
                    return $"file:///{coverPath.Replace("\\", "/").Replace(" ", "%20")}";
                }
                return null;
            }
        }
    }
}
