using System.IO;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace SamsGameLauncher.Models
{
    public class GameLibrary
    {
        // Backing collections for emulators and games
        public List<Emulator> Emulators { get; set; } = new();
        public ObservableCollection<GameBase> Games { get; set; } = new();

        // Load from JSON files and wire up emulator references
        public void LoadData(string emulatorPath, string gamePath)
        {
            // Read and deserialize emulators.json
            string emuJson = File.ReadAllText(emulatorPath);
            Emulators = JsonSerializer.Deserialize<List<Emulator>>(emuJson)
                        ?? new List<Emulator>();

            // Read and deserialize games.json using our custom converter
            string gameJson = File.ReadAllText(gamePath);
            var options = new JsonSerializerOptions();
            options.Converters.Add(new GameJsonConverter());
            var loadedGames = JsonSerializer
                .Deserialize<List<GameBase>>(gameJson, options)
                ?? new List<GameBase>();

            // Populate the ObservableCollection for binding
            Games = new ObservableCollection<GameBase>(loadedGames);

            // For each emulated game, find its Emulator by ID
            foreach (var emGame in Games.OfType<EmulatedGame>())
                emGame.Emulator = Emulators.FirstOrDefault(e => e.Id == emGame.EmulatorId);

            // For each folder‑based game, do the same
            foreach (var fbGame in Games.OfType<FolderBasedGame>())
                fbGame.Emulator = Emulators.FirstOrDefault(e => e.Id == fbGame.EmulatorId);
        }

        // Ensure JSON files exist (copy from /dev or create empty), then load
        public void InitializeAndLoadData(string dataFolder)
        {
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            string emulatorPath = Path.Combine(dataFolder, "emulators.json");
            string gamePath = Path.Combine(dataFolder, "games.json");

            // Local helper to copy from a dev file or create an empty array
            void EnsureFile(string targetPath, string devFileName)
            {
                if (File.Exists(targetPath))
                    return;

                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string devPath = Path.Combine(basePath, "dev", devFileName);

                if (File.Exists(devPath) && new FileInfo(devPath).Length > 0)
                {
                    File.Copy(devPath, targetPath);
                }
                else
                {
                    // Create an empty JSON array if dev file missing or empty
                    File.WriteAllText(targetPath, "[]");
                }
            }

            EnsureFile(emulatorPath, "dev_emulators.json");
            EnsureFile(gamePath, "dev_games.json");

            // Now load from disk
            LoadData(emulatorPath, gamePath);
        }

        // Serialize and save the Games collection to disk
        public void SaveGames(string gamePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(new GameJsonConverter());
            string gameJson = JsonSerializer.Serialize(Games, options);
            File.WriteAllText(gamePath, gameJson);
        }

        // Serialize and save the Emulators list to disk
        public void SaveEmulators(string emulatorPath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string emuJson = JsonSerializer.Serialize(Emulators, options);
            File.WriteAllText(emulatorPath, emuJson);
        }
    }
}
