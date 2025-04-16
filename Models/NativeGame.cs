using System.IO;
using System.Text.Json.Serialization;
using SamsGameLauncher.Utilities;

namespace SamsGameLauncher.Models
{
    public class NativeGame : GameBase
    {
        // Full path to the game’s executable
        public string ExePath { get; set; } = string.Empty;

        // Identifies this as the native subtype
        public override GameType GameType => GameType.Native;

        // At runtime, looks for a cover next to the EXE named like "{BaseName}.png/.jpg/etc."
        [JsonIgnore]
        public override string GameCoverUri
        {
            get
            {
                if (string.IsNullOrEmpty(ExePath))
                    return string.Empty;

                var directory = Path.GetDirectoryName(ExePath);
                if (string.IsNullOrEmpty(directory))
                    return string.Empty;

                var baseName = Path.GetFileNameWithoutExtension(ExePath);
                var coverPath = GameCoverLocator.FindGameCover(directory, baseName);

                if (string.IsNullOrEmpty(coverPath))
                    return string.Empty;

                // Let Uri handle proper escaping and "file:///" prefix
                return new Uri(coverPath).AbsoluteUri;
            }
        }
    }
}
