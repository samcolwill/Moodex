using System.IO;
using System.Text.Json;
using System.Linq;
using SamsGameLauncher.Models;
using System.Collections.ObjectModel;

namespace SamsGameLauncher.Models
{
    public class GameLibrary
    {
        public List<Emulator> Emulators { get; set; } = new();
        public ObservableCollection<Game> Games { get; set; } = new();

        public void LoadData(string emulatorPath, string gamePath)
        {
            string emuJson = File.ReadAllText(emulatorPath);
            Emulators = JsonSerializer.Deserialize<List<Emulator>>(emuJson) ?? new List<Emulator>();

            string gameJson = File.ReadAllText(gamePath);

            var loadedGames = JsonSerializer.Deserialize<List<Game>>(gameJson) ?? new List<Game>();
            Games = new ObservableCollection<Game>(loadedGames);

            // Link emulators to games
            foreach (var game in Games)
            {
                game.Emulator = Emulators.FirstOrDefault(e => e.Id == game.EmulatorId);
            }
        }

        // New method: check and create the Data folder and default JSON files if needed, then load the data.
        public void InitializeAndLoadData(string dataFolder)
        {
            // Ensure the data folder exists
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            // Define paths for the JSON files.
            string emulatorPath = Path.Combine(dataFolder, "emulators.json");
            string gamePath = Path.Combine(dataFolder, "games.json");

            // If the emulator file doesn't exist, create it with an empty array (or add defaults as needed)
            if (!File.Exists(emulatorPath))
            {
                File.WriteAllText(emulatorPath, "[]");
            }

            // Likewise for the games file.
            if (!File.Exists(gamePath))
            {
                File.WriteAllText(gamePath, "[]");
            }

            // Now load the JSON data.
            LoadData(emulatorPath, gamePath);
        }

        // Method to save the current Games list back to the JSON file.
        public void SaveGames(string gamePath)
        {
            string gameJson = JsonSerializer.Serialize(Games, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(gamePath, gameJson);
        }

        public void SaveEmulators(string emulatorPath)
        {
            string emuJson = JsonSerializer.Serialize(Emulators, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(emulatorPath, emuJson);
        }

    }
}
