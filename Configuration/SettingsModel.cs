using SamsGameLauncher.Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace SamsGameLauncher.Configuration
{
    public class SettingsModel
    {
        public int DefaultMonitorIndex { get; set; } = 0;
        public string LaunchMode { get; set; } = "Fullscreen";
        public int ResolutionWidth { get; set; } = 1920;
        public int ResolutionHeight { get; set; } = 1080;
        public string ActiveLibraryPath { get; set; } = @"C:\Game Library";
        public string ArchiveLibraryPath { get; set; } = @"M:\Game Library";
        public LibraryKind PrimaryLibrary { get; set; } = LibraryKind.Active;
        public List<ConsoleInfo> Consoles { get; set; } = new List<ConsoleInfo>
        {
            new ConsoleInfo { Id="gameboyadvance", Name="Game Boy Advance" },
            new ConsoleInfo { Id="nintendoswitch", Name="Nintendo Switch" },
            new ConsoleInfo { Id="pc", Name="PC" },
            new ConsoleInfo { Id="playstation1", Name="Playstation 1" },
            new ConsoleInfo { Id="playstation2", Name="Playstation 2" },
            new ConsoleInfo { Id="playstation3", Name="Playstation 3" },
            new ConsoleInfo { Id="xbox", Name="Xbox" }
        };
        public List<string> Genres { get; set; } = new List<string>
        {
            "Action",
            "Adventure",
            "Fighting",
            "Platform",
            "Puzzle",
            "Racing",
            "Role-playing",
            "Shooter",
            "Simulation",
            "Sports",
            "Strategy"
        };
        public bool IsDs4Installed { get; set; } = false;
        public bool LaunchDs4WindowsOnStartup { get; set; } = false;
    }

    public enum LibraryKind
    {
        Active,   // the drive you launch from
        Archive   // long‑term storage
    }
}
