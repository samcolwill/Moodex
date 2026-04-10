namespace Moodex.Configuration
{
    public class SteamSettings
    {
        public bool Enabled { get; set; }
        public string SteamExePath { get; set; } = string.Empty;
        public List<string> LibraryPaths { get; set; } = new();
        public bool AutoScanOnStartup { get; set; }
        public string SteamApiKey { get; set; } = string.Empty;
        public string SteamId64 { get; set; } = string.Empty;
        public List<string> IgnoredGames { get; set; } = new();
    }
}
