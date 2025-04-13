using System.Text.Json.Serialization;

namespace SamsGameLauncher.Models
{
    public class Game
    {
        public string Name { get; set; }
        public string EmulatorId { get; set; }
        public string GamePath { get; set; }
        public string GameCoverPath { get; set; }
        public string ImageUri => $"file:///{GameCoverPath.Replace("\\", "/").Replace(" ", "%20")}";
        public string Console { get; set; }
        public string Genre { get; set; }
        public DateTime ReleaseDate { get; set; }


        // Runtime properties, they don't persist in games.json.
        [JsonIgnore]
        public int ReleaseYear => ReleaseDate.Year;
        [JsonIgnore]
        public Emulator Emulator { get; set; }
    }
}
