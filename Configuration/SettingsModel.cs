using Moodex.Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Moodex.Configuration
{
    public class SettingsModel
    {
        public int DefaultMonitorIndex { get; set; } = 0;
        public string LaunchMode { get; set; } = "Fullscreen";
        public int ResolutionWidth { get; set; } = 1920;
        public int ResolutionHeight { get; set; } = 1080;
        public string ActiveLibraryPath { get; set; } = @"C:\Moodex Library";
        public string ArchiveLibraryPath { get; set; } = @"M:\Moodex Archive";
        public List<ConsoleInfo> Consoles { get; set; } = new List<ConsoleInfo>
        {
            new ConsoleInfo { Id="gameboy", Name="Game Boy" },
            new ConsoleInfo { Id="pc", Name="PC" },
            new ConsoleInfo { Id="playstation1", Name="Playstation 1" },
            new ConsoleInfo { Id="playstation2", Name="Playstation 2" },
            new ConsoleInfo { Id="xbox", Name="Xbox"}
        };
        public List<string> Genres { get; set; } = new List<string>
        {
            "Action",
            "Action-Adventure",
            "Adventure",
            "Co-op",
            "Fighting",
            "First-Person Shooter",
            "Hack and Slash",
            "Horror",
            "Platform",
            "Puzzle",
            "Racing",
            "Role-Playing Game",
            "Simulation",
            "Sports",
            "Stealth",
            "Strategy",
            "Survival",
            "Third-Person Shooter",
            "Turn-Based Strategy"
        };
        public bool IsDs4Installed { get; set; } = false;
        public bool IsAutoHotKeyInstalled { get; set; } = false;
        public bool CompressOnArchive { get; set; } = true;
        public string DefaultGroupBy { get; set; } = "Console";
    }
}

