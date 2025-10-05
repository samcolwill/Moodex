using SamsGameLauncher.Utilities;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;

namespace SamsGameLauncher.Models
{
    public class GameInfo
    {
        // Game data loaded/saved to games.json
        public string Name { get; set; } = string.Empty;
        public string ConsoleId { get; set; } = string.Empty;
        public string FileSystemPath { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; } = DateTime.MinValue;
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool HasAutoHotKeyScript { get; set; } = false;

        // Runtime-only helpers
        [JsonIgnore]
        public string? ConsoleName => Utilities.ConsoleRegistry.GetDisplayName(ConsoleId);
        [JsonIgnore]
        public string? GameCoverUri => GameCoverLocator.FindGameCover(this);
        [JsonIgnore]
        public int ReleaseYear => ReleaseDate.Year;
        [JsonIgnore]
        public bool IsInArchive { get; set; }
    }
}