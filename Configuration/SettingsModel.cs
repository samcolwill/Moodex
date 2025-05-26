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
        public List<ConsoleType> Consoles { get; set; } = Enum.GetValues<ConsoleType>().ToList();
        public bool IsDs4Installed { get; set; } = false;
        public bool LaunchDs4WindowsOnStartup { get; set; } = false;
    }

    public enum LibraryKind
    {
        Active,   // the drive you launch from
        Archive   // long‑term storage
    }
}
