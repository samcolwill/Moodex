using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;

namespace Moodex.Services.Steam
{
    public class SteamDetectionService : ISteamDetectionService
    {
        public SteamInstallationInfo? DetectSteamInstallation()
        {
            var steamPath = GetSteamPathFromRegistry();
            if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath))
                return null;

            var steamExe = Path.Combine(steamPath, "steam.exe");
            if (!File.Exists(steamExe))
                return null;

            var libraryPaths = DiscoverLibraryFolders(steamPath);
            return new SteamInstallationInfo(steamExe, libraryPaths);
        }

        public List<SteamGameEntry> ScanInstalledGames(List<string> libraryPaths)
        {
            var games = new List<SteamGameEntry>();

            foreach (var libPath in libraryPaths)
            {
                var steamapps = Path.Combine(libPath, "steamapps");
                if (!Directory.Exists(steamapps))
                    continue;

                foreach (var acfFile in Directory.GetFiles(steamapps, "appmanifest_*.acf"))
                {
                    var entry = ParseAcfFile(acfFile, libPath);
                    if (entry != null)
                        games.Add(entry);
                }
            }

            return games;
        }

        public List<SteamGameEntry> ResolveInstallStatus(List<SteamGameEntry> ownedGames, List<string> libraryPaths)
        {
            var localByAppId = new Dictionary<int, SteamGameEntry>();
            foreach (var g in ScanInstalledGames(libraryPaths))
                localByAppId[g.AppId] = g;

            var result = new List<SteamGameEntry>(ownedGames.Count);
            foreach (var owned in ownedGames)
            {
                if (localByAppId.TryGetValue(owned.AppId, out var local))
                    result.Add(local);
                else
                    result.Add(owned);
            }
            return result;
        }

        public string? DetectSteamId64(string steamPath)
        {
            // Primary: parse loginusers.vdf for the most recent user
            var loginUsersPath = Path.Combine(steamPath, "config", "loginusers.vdf");
            if (File.Exists(loginUsersPath))
            {
                try
                {
                    var content = File.ReadAllText(loginUsersPath);
                    // Find all user blocks: "76561..." { ... "mostrecent" "1" ... }
                    var userBlocks = Regex.Matches(content, @"""(\d{17})""\s*\{([^}]*)\}", RegexOptions.Singleline);
                    foreach (Match block in userBlocks)
                    {
                        var body = block.Groups[2].Value;
                        var mostRecent = Regex.Match(body, @"""mostrecent""\s+""1""", RegexOptions.IgnoreCase);
                        if (mostRecent.Success)
                            return block.Groups[1].Value;
                    }
                }
                catch { }
            }

            // Fallback: registry ActiveUser (32-bit account ID) -> SteamID64
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam\ActiveProcess");
                var activeUser = key?.GetValue("ActiveUser");
                if (activeUser is int userId && userId > 0)
                    return (76561197960265728L + userId).ToString();
                if (activeUser is uint userIdU && userIdU > 0)
                    return (76561197960265728L + userIdU).ToString();
            }
            catch { }

            return null;
        }

        private static string? GetSteamPathFromRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                return key?.GetValue("SteamPath") as string;
            }
            catch
            {
                return null;
            }
        }

        private static List<string> DiscoverLibraryFolders(string steamPath)
        {
            var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var paths = new List<string>();

            if (File.Exists(vdfPath))
            {
                try
                {
                    var content = File.ReadAllText(vdfPath);
                    var matches = Regex.Matches(content, @"""path""\s+""([^""]+)""");
                    foreach (Match m in matches)
                    {
                        var p = Path.GetFullPath(m.Groups[1].Value.Replace(@"\\", @"\"));
                        if (Directory.Exists(p) && normalized.Add(p))
                            paths.Add(p);
                    }
                }
                catch { }
            }

            if (paths.Count == 0)
            {
                var fallback = Path.GetFullPath(steamPath);
                if (Directory.Exists(fallback))
                    paths.Add(fallback);
            }

            return paths;
        }

        private static SteamGameEntry? ParseAcfFile(string acfPath, string libraryPath)
        {
            try
            {
                var content = File.ReadAllText(acfPath);

                var appIdMatch = Regex.Match(content, @"""appid""\s+""(\d+)""");
                var nameMatch = Regex.Match(content, @"""name""\s+""([^""]+)""");
                var installDirMatch = Regex.Match(content, @"""installdir""\s+""([^""]+)""");
                var stateMatch = Regex.Match(content, @"""StateFlags""\s+""(\d+)""");

                if (!appIdMatch.Success || !nameMatch.Success)
                    return null;

                var appId = int.Parse(appIdMatch.Groups[1].Value);
                var name = nameMatch.Groups[1].Value;
                var installDir = installDirMatch.Success ? installDirMatch.Groups[1].Value : string.Empty;
                var stateFlags = stateMatch.Success ? int.Parse(stateMatch.Groups[1].Value) : 0;

                // StateFlags 4 = fully installed
                var isInstalled = (stateFlags & 4) != 0;
                var fullInstallPath = Path.Combine(libraryPath, "steamapps", "common", installDir);

                return new SteamGameEntry(appId, name, fullInstallPath, libraryPath, isInstalled);
            }
            catch
            {
                return null;
            }
        }
    }
}
