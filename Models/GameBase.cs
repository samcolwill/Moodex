using System.Text.Json.Serialization;

namespace SamsGameLauncher.Models
{
    // Uses GameJsonConverter to pick the right subclass at runtime
    [JsonConverter(typeof(GameJsonConverter))]
    public abstract class GameBase
    {
        // Core data loaded/saved in JSON
        public string Name { get; set; } = string.Empty;
        public string Console { get; set; } = string.Empty;
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
    }
}