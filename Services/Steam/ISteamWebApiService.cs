namespace Moodex.Services.Steam
{
    public interface ISteamWebApiService
    {
        Task<List<SteamGameEntry>> GetOwnedGamesAsync(string apiKey, string steamId64);
    }
}
