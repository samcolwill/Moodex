namespace Moodex.Services.Steam
{
    public record SteamImportResult(int Imported, int Removed, int Unchanged);

    public interface ISteamImportService
    {
        SteamImportResult ImportSteamGames(string libraryRoot, List<SteamGameEntry> steamGames, string? steamPath = null, IReadOnlyCollection<string>? ignoredNames = null);
    }
}
