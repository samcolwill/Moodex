using System;
using System.Text.Json.Serialization;

namespace SamsGameLauncher.Models
{
    // Note: The GameJsonConverter handles polymorphic serialization.
    [JsonConverter(typeof(GameJsonConverter))]
    public abstract class GameBase
    {
        public string Name { get; set; }
        public string Console { get; set; }
        public string Genre { get; set; }
        public DateTime ReleaseDate { get; set; }
        public abstract GameType GameType { get; }

        // Runtime variables
        [JsonIgnore]
        public abstract string GameCoverUri { get; }
        [JsonIgnore]
        public int ReleaseYear => ReleaseDate.Year;
    }
}