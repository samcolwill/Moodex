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
        public List<string> Consoles { get; set; } = new List<string>
        {
            "PC",
            "PlayStation 1",
            "PlayStation 2",
            "PlayStation 3",
            "Game Boy",
            "Game Boy Color",
            "Game Boy Advance",
            "Nintendo Switch"
        };
    }



    public enum LibraryKind
    {
        Active,   // the drive you launch from
        Archive   // long‑term storage
    }
}
