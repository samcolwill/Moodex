using System.IO;
using System.Text.Json;
using System.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SamsGameLauncher.Models
{
    public class GameLibrary
    {
        public List<Emulator> Emulators { get; set; } = new();
        public ObservableCollection<GameBase> Games { get; set; } = new();

        public void LoadData(string emulatorPath, string gamePath)
        {
            // Load emulators
            string emuJson = File.ReadAllText(emulatorPath);
            Emulators = JsonSerializer.Deserialize<List<Emulator>>(emuJson) ?? new List<Emulator>();

            // Load games
            string gameJson = File.ReadAllText(gamePath);
            var options = new JsonSerializerOptions();
            options.Converters.Add(new GameJsonConverter());
            var loadedGames = JsonSerializer.Deserialize<List<GameBase>>(gameJson, options) ?? new List<GameBase>();
            Games = new ObservableCollection<GameBase>(loadedGames);

            // Link emulators to emulated games
            foreach (var game in Games.OfType<EmulatedGame>())
            {
                game.Emulator = Emulators.FirstOrDefault(e => e.Id == game.EmulatorId);
            }
            foreach (var game in Games.OfType<FolderBasedGame>())
            {
                game.Emulator = Emulators.FirstOrDefault(e => e.Id == game.EmulatorId);
            }
        }

        public void InitializeAndLoadData(string dataFolder)
        {
            // Ensure the data folder exists
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            // Define paths for the runtime JSON files
            string emulatorPath = Path.Combine(dataFolder, "emulators.json");
            string gamePath = Path.Combine(dataFolder, "games.json");

            // Ensure emulators.json exists
            if (!File.Exists(emulatorPath))
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                // dev_emulators.json is expected in bin\Debug\netXX-windows\dev\dev_emulators.json
                string devEmulatorPath = Path.Combine(basePath, "dev", "dev_emulators.json");
                Debug.WriteLine($"[InitializeAndLoadData] Looking for dev_emulators.json at: {devEmulatorPath}");

                if (File.Exists(devEmulatorPath))
                {
                    long fileLength = new FileInfo(devEmulatorPath).Length;
                    Debug.WriteLine($"[InitializeAndLoadData] Found dev_emulators.json with length: {fileLength} bytes");

                    if (fileLength > 0)
                    {
                        File.Copy(devEmulatorPath, emulatorPath);
                        Debug.WriteLine($"[InitializeAndLoadData] Copied dev_emulators.json to {emulatorPath}");
                    }
                    else
                    {
                        Debug.WriteLine("[InitializeAndLoadData] dev_emulators.json is empty. Creating empty emulators.json.");
                        File.WriteAllText(emulatorPath, "[]");
                    }
                }
                else
                {
                    Debug.WriteLine("[InitializeAndLoadData] dev_emulators.json not found. Creating empty emulators.json.");
                    File.WriteAllText(emulatorPath, "[]");
                }
            }

            // Ensure games.json exists
            if (!File.Exists(gamePath))
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                // dev_games.json is expected in bin\Debug\netXX-windows\dev\dev_games.json
                string devGamePath = Path.Combine(basePath, "dev", "dev_games.json");
                Debug.WriteLine($"[InitializeAndLoadData] Looking for dev_games.json at: {devGamePath}");

                if (File.Exists(devGamePath))
                {
                    long fileLength = new FileInfo(devGamePath).Length;
                    Debug.WriteLine($"[InitializeAndLoadData] Found dev_games.json with length: {fileLength} bytes");

                    if (fileLength > 0)
                    {
                        File.Copy(devGamePath, gamePath);
                        Debug.WriteLine($"[InitializeAndLoadData] Copied dev_games.json to {gamePath}");
                    }
                    else
                    {
                        Debug.WriteLine("[InitializeAndLoadData] dev_games.json is empty. Creating empty games.json.");
                        File.WriteAllText(gamePath, "[]");
                    }
                }
                else
                {
                    Debug.WriteLine("[InitializeAndLoadData] dev_games.json not found. Creating empty games.json.");
                    File.WriteAllText(gamePath, "[]");
                }
            }

            // Finally, load the data
            LoadData(emulatorPath, gamePath);
        }

        public void SaveGames(string gamePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(new GameJsonConverter());
            string gameJson = JsonSerializer.Serialize(Games, options);
            File.WriteAllText(gamePath, gameJson);
        }

        public void SaveEmulators(string emulatorPath)
        {
            string emuJson = JsonSerializer.Serialize(Emulators, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(emulatorPath, emuJson);
        }
    }
}
