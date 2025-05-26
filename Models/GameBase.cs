using System.IO;
using System.Text.Json.Serialization;

namespace SamsGameLauncher.Models
{
    // Uses GameJsonConverter to pick the right subclass at runtime
    [JsonConverter(typeof(GameJsonConverter))]
    public abstract class GameBase
    {
        // Core data loaded/saved in JSON
        public string Name { get; set; } = string.Empty;
        public ConsoleType Console { get; set; } = ConsoleType.None;
        [JsonIgnore]
        public string ConsoleName => Console.GetDescription();
        public string Genre { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }

        // Indicates which concrete type this is
        public abstract GameType GameType { get; }

        // ─────────── Runtime-only helpers ───────────

        // Not serialized—points to where we find the cover image
        [JsonIgnore]
        public abstract string GameCoverUri { get; }

        // Convenience for grouping by year
        [JsonIgnore]
        public int ReleaseYear => ReleaseDate.Year;

        // ─── Quick‑and‑dirty thumbnail lookup ───
        [JsonIgnore]
        public string ThumbnailPath
        {
            get
            {
                // Determine the folder where the game’s files live:
                //   - EmulatedGame stores its ROM at GamePath
                //   - FolderBasedGame stores a folder path
                //   - NativeGame stores its EXE at ExePath
                string folder = GameType switch
                {
                    GameType.Emulated => Path.GetDirectoryName(((EmulatedGame)this).GamePath) ?? "",
                    GameType.FolderBased => ((FolderBasedGame)this).FolderPath,
                    GameType.Native => Path.GetDirectoryName(((NativeGame)this).ExePath) ?? "",
                    _ => ""
                };

                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                    return "";

                // Try common image extensions in order
                foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
                {
                    var candidate = Path.Combine(folder, Name + ext);
                    if (File.Exists(candidate))
                        return candidate;
                }

                return "";
            }
        }

        [JsonIgnore]                 // not persisted
        public bool IsInArchive { get; set; }
    }
}