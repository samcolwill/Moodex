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
        }

        // Ensure JSON files exist (copy from /dev or create empty), then load
        public void InitializeAndLoadData(string dataFolder)
        {
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            string emulatorPath = Path.Combine(dataFolder, "emulators.json");
            string gamePath = Path.Combine(dataFolder, "games.json");

            // ensure each target file exists, seeding from your dev samples if present
            SeedDataFileFromDev(emulatorPath, "dev_emulators.json");
            SeedDataFileFromDev(gamePath, "dev_games.json");

            // now load them for real
            LoadData(emulatorPath, gamePath);
        }

        private void SeedDataFileFromDev(string targetPath, string devSampleFileName)
        {
            if (File.Exists(targetPath))
                return;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var devPath = Path.Combine(baseDir, "dev", devSampleFileName);

            if (File.Exists(devPath) && new FileInfo(devPath).Length > 0)
            {
                // copy in the sample data
                File.Copy(devPath, targetPath);
            }
            else
            {
                // fall back to an empty array
                File.WriteAllText(targetPath, "[]");
            }
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
