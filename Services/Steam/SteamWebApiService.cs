using System.Net.Http;
using System.Text.Json;

namespace Moodex.Services.Steam
{
    public class SteamWebApiService : ISteamWebApiService
    {
        private static readonly HttpClient Http = new();

        public async Task<List<SteamGameEntry>> GetOwnedGamesAsync(string apiKey, string steamId64)
        {
            var url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/" +
                      $"?key={apiKey}&steamid={steamId64}" +
                      $"&include_appinfo=true&include_played_free_games=true&format=json";

            var json = await Http.GetStringAsync(url).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            var response = doc.RootElement.GetProperty("response");

            if (!response.TryGetProperty("games", out var gamesArray))
                return new List<SteamGameEntry>();

            var results = new List<SteamGameEntry>();
            foreach (var game in gamesArray.EnumerateArray())
            {
                var appId = game.GetProperty("appid").GetInt32();
                var name = game.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? $"App {appId}" : $"App {appId}";

                results.Add(new SteamGameEntry(appId, name, string.Empty, string.Empty, false));
            }

            return results;
        }
    }
}
