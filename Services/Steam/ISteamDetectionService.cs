namespace Moodex.Services.Steam
{
    public record SteamInstallationInfo(string SteamExePath, List<string> LibraryPaths);

    public record SteamGameEntry(int AppId, string Name, string InstallDir, string LibraryPath, bool IsInstalled);

    public interface ISteamDetectionService
    {
        SteamInstallationInfo? DetectSteamInstallation();
        List<SteamGameEntry> ScanInstalledGames(List<string> libraryPaths);
        string? DetectSteamId64(string steamPath);
        List<SteamGameEntry> ResolveInstallStatus(List<SteamGameEntry> ownedGames, List<string> libraryPaths);
    }
}
