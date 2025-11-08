using System.IO;
using System.Text.Json;
using Moodex.Configuration;
using Moodex.Models;
using Moodex.Models.Manifests;

namespace Moodex.Services
{
    public interface ILibraryScanner
    {
        (List<EmulatorInfo> emulators, List<GameInfo> games) Scan(string libraryRoot);
    }

    public class LibraryScanner : ILibraryScanner
    {
        public (List<EmulatorInfo> emulators, List<GameInfo> games) Scan(string libraryRoot)
        {
            var emulators = new List<EmulatorInfo>();
            var games = new List<GameInfo>();

            // Emulators
            var emulatorsRoot = Path.Combine(libraryRoot, "Emulators");
            if (Directory.Exists(emulatorsRoot))
            {
                foreach (var emuDir in Directory.GetDirectories(emulatorsRoot))
                {
                    var manifestPath = Path.Combine(emuDir, ".moodex_emulator");
                    if (!File.Exists(manifestPath)) continue;
                    try
                    {
                        var json = File.ReadAllText(manifestPath);
                        var man = JsonSerializer.Deserialize<EmulatorManifest>(json);
                        if (man == null) continue;

                        emulators.Add(new EmulatorInfo
                        {
                            Id = man.Id,
                            Name = man.Name,
                            Guid = man.Guid,
                            EmulatedConsoleIds = man.EmulatedConsoleIds ?? new List<string>(),
                            ExecutablePath = Path.IsPathRooted(man.ExecutablePath)
                                ? man.ExecutablePath
                                : Path.Combine(emuDir, man.ExecutablePath),
                            DefaultArguments = man.DefaultArguments
                        });
                    }
                    catch { /* skip malformed */ }
                }
            }

            // Games
            var gamesRoot = Path.Combine(libraryRoot, "Games");
            if (Directory.Exists(gamesRoot))
            {
                foreach (var platformDir in Directory.GetDirectories(gamesRoot))
                {
                    foreach (var gameDir in Directory.GetDirectories(platformDir))
                    {
                        var manifestPath = Path.Combine(gameDir, ".moodex_game");
                        if (!File.Exists(manifestPath)) continue;
                        try
                        {
                            var json = File.ReadAllText(manifestPath);
                            var man = JsonSerializer.Deserialize<GameManifest>(json);
                            if (man == null) continue;

                            var dataDir = Path.Combine(gameDir, "data");
                            var launchPath = man.LaunchType.Equals("file", StringComparison.OrdinalIgnoreCase)
                                ? Path.Combine(dataDir, man.LaunchTarget)
                                : Path.Combine(dataDir, man.LaunchTarget);

                            var hasGenres = (man.Genres?.Count ?? 0) > 0;
                            var genre = hasGenres ? string.Join(", ", man.Genres!) : string.Empty;

                            var gi = new GameInfo
                            {
                                Name = man.Name,
                                ConsoleId = man.ConsoleId,
                                FileSystemPath = launchPath,
                                Genre = genre,
                                ReleaseDate = man.ReleaseDateTime ?? DateTime.MinValue,
                                IsInArchive = man.Archived
                            };
                            // extra runtime fields
                            gi.GameRootPath = gameDir;
                            gi.GameGuid = man.Guid;
                            gi.LaunchTarget = man.LaunchTarget;
                            // completion flags
                            gi.CompletedAnyPercent = man.CompletionAnyPercent;
                            gi.CompletedMaxDifficulty = man.CompletionMaxDifficulty;
                            gi.CompletedHundredPercent = man.CompletionHundredPercent;
                            gi.HasAchievements = man.HasAchievements;

                            // input scripts from manifest
                            if (man.InputScripts != null)
                            {
                                foreach (var s in man.InputScripts)
                                {
                                    gi.InputScripts.Add(new InputScriptInfo { Name = s.Name, Enabled = s.Enabled });
                                }
                                gi.HasAutoHotKeyScript = gi.InputScripts.Count > 0;
                            }

                            // controller settings
                            gi.ControllerEnabled = man.ControllerEnabled;
                            gi.ControllerProfileConfigured = man.ControllerProfileConfigured;

                            games.Add(gi);
                        }
                        catch { /* skip malformed */ }
                    }
                }
            }

            return (emulators, games);
        }
    }
}


