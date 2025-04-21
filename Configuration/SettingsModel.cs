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
    }

    public enum LibraryKind
    {
        Active,   // the drive you launch from
        Archive   // long‑term storage
    }
}
