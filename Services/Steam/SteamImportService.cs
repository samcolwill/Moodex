using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Moodex.Models.Manifests;

namespace Moodex.Services.Steam
{
    public class SteamImportService : ISteamImportService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public SteamImportResult ImportSteamGames(string libraryRoot, List<SteamGameEntry> steamGames, string? steamPath = null, IReadOnlyCollection<string>? ignoredNames = null)
        {
            var steamPlatformDir = Path.Combine(libraryRoot, "Games", "Steam");
            Directory.CreateDirectory(steamPlatformDir);

            var ignored = ignoredNames != null
                ? new HashSet<string>(ignoredNames, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var existingByAppId = IndexExistingSteamGames(steamPlatformDir);
            var scannedAppIds = new HashSet<int>();
            int imported = 0;
            int unchanged = 0;

            foreach (var entry in steamGames)
            {
                if (ignored.Contains(entry.Name))
                    continue;

                scannedAppIds.Add(entry.AppId);

                if (existingByAppId.ContainsKey(entry.AppId))
                {
                    UpdateInstallState(existingByAppId[entry.AppId], entry);
                    TryCopyCoverArt(Path.GetDirectoryName(existingByAppId[entry.AppId])!, entry.AppId, steamPath);
                    unchanged++;
                    continue;
                }

                var gameDir = CreateSteamGameEntry(steamPlatformDir, entry);
                TryCopyCoverArt(gameDir, entry.AppId, steamPath);
                imported++;
            }

            int removed = 0;
            foreach (var (appId, manifestPath) in existingByAppId)
            {
                if (!scannedAppIds.Contains(appId))
                {
                    var gameDir = Path.GetDirectoryName(manifestPath);
                    if (gameDir != null && Directory.Exists(gameDir))
                    {
                        try { Directory.Delete(gameDir, recursive: true); } catch { }
                        removed++;
                    }
                }
            }

            return new SteamImportResult(imported, removed, unchanged);
        }

        private static string CreateSteamGameEntry(string steamPlatformDir, SteamGameEntry entry)
        {
            var safeName = SanitizeFolderName(entry.Name);
            var gameDir = Path.Combine(steamPlatformDir, safeName);
            Directory.CreateDirectory(gameDir);

            var manifest = new GameManifest
            {
                Name = entry.Name,
                Guid = System.Guid.NewGuid().ToString(),
                AddedDateTime = DateTime.UtcNow,
                ConsoleId = "pc",
                LaunchTarget = string.Empty,
                LaunchType = "file",
                Source = "steam",
                SteamAppId = entry.AppId,
                SteamInstallPath = entry.InstallDir,
                SteamInstallState = entry.IsInstalled ? "installed" : "not_installed"
            };

            var json = JsonSerializer.Serialize(manifest, JsonOptions);
            File.WriteAllText(Path.Combine(gameDir, ".moodex_game"), json);
            return gameDir;
        }

        private static void UpdateInstallState(string manifestPath, SteamGameEntry entry)
        {
            try
            {
                var json = File.ReadAllText(manifestPath);
                var man = JsonSerializer.Deserialize<GameManifest>(json);
                if (man == null) return;

                var newState = entry.IsInstalled ? "installed" : "not_installed";
                if (man.SteamInstallState == newState && man.SteamInstallPath == entry.InstallDir)
                    return;

                man.SteamInstallState = newState;
                man.SteamInstallPath = entry.InstallDir;
                File.WriteAllText(manifestPath, JsonSerializer.Serialize(man, JsonOptions));
            }
            catch { }
        }

        private static Dictionary<int, string> IndexExistingSteamGames(string steamPlatformDir)
        {
            var index = new Dictionary<int, string>();
            if (!Directory.Exists(steamPlatformDir)) return index;

            foreach (var gameDir in Directory.GetDirectories(steamPlatformDir))
            {
                var manifestPath = Path.Combine(gameDir, ".moodex_game");
                if (!File.Exists(manifestPath)) continue;
                try
                {
                    var json = File.ReadAllText(manifestPath);
                    var man = JsonSerializer.Deserialize<GameManifest>(json);
                    if (man?.SteamAppId != null && man.Source == "steam")
                        index[man.SteamAppId.Value] = manifestPath;
                }
                catch { }
            }

            return index;
        }

        private static void TryCopyCoverArt(string gameDir, int appId, string? steamPath)
        {
            if (string.IsNullOrEmpty(steamPath)) return;

            var destCover = Path.Combine(gameDir, "cover.jpg");
            if (File.Exists(destCover)) return;

            var candidates = new[]
            {
                Path.Combine(steamPath, "appcache", "librarycache", $"{appId}_library_600x900.jpg"),
                Path.Combine(steamPath, "appcache", "librarycache", $"{appId}_library_600x900.png"),
                Path.Combine(steamPath, "appcache", "librarycache", $"{appId}_header.jpg"),
            };

            foreach (var src in candidates)
            {
                if (!File.Exists(src)) continue;
                try
                {
                    File.Copy(src, destCover, overwrite: false);
                    return;
                }
                catch { }
            }
        }

        private static string SanitizeFolderName(string name)
        {
            var invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var sanitized = Regex.Replace(name, $"[{Regex.Escape(invalid)}]", "").Trim();
            if (string.IsNullOrWhiteSpace(sanitized)) sanitized = "Unknown";
            return sanitized;
        }
    }
}
