using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Moodex.Services.Igdb
{
    public class IgdbService : IIgdbService
    {
        private const string ClientId = "mqs2x95mxchzwvzftmscngxbov2jti";
        private const string ClientSecret = "e73e3s9ol6e79xciscv38tetlhfolw";
        private const string TokenUrl = "https://id.twitch.tv/oauth2/token";
        private const string ApiBaseUrl = "https://api.igdb.com/v4";
        private const string ImageBaseUrl = "https://images.igdb.com/igdb/image/upload";

        private readonly ISettingsService _settingsService;
        private readonly HttpClient _httpClient = new();

        private string? _accessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public IgdbService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public bool IsEnabled => _settingsService.Load().Igdb.Enabled;

        public async Task<List<IgdbGameResult>> SearchGameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<IgdbGameResult>();

            await EnsureTokenAsync();

            var escaped = name.Replace("\"", "\\\"");
            var body = $"search \"{escaped}\"; fields name, genres.name, first_release_date, cover.image_id, summary; limit 10;";

            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/games")
            {
                Content = new StringContent(body)
            };
            request.Headers.Add("Client-ID", ClientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"IGDB API {(int)response.StatusCode} {response.StatusCode}: {json}");
            var games = JsonSerializer.Deserialize<List<IgdbRawGame>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (games == null) return new List<IgdbGameResult>();

            return games.Select(g => new IgdbGameResult
            {
                Id = g.Id,
                Name = g.Name ?? string.Empty,
                Genres = g.Genres?.Select(genre => genre.Name ?? string.Empty)
                                  .Where(n => !string.IsNullOrEmpty(n))
                                  .ToList() ?? new List<string>(),
                ReleaseDate = g.First_release_date.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(g.First_release_date.Value).DateTime
                    : null,
                CoverImageId = g.Cover?.Image_id,
                Summary = g.Summary
            }).ToList();
        }

        public async Task<byte[]?> DownloadCoverAsync(string imageId)
        {
            if (string.IsNullOrWhiteSpace(imageId))
                return null;

            var url = $"{ImageBaseUrl}/t_cover_big/{imageId}.jpg";
            try
            {
                return await _httpClient.GetByteArrayAsync(url);
            }
            catch
            {
                return null;
            }
        }

        private async Task EnsureTokenAsync()
        {
            if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
                return;

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["grant_type"] = "client_credentials"
            });

            var response = await _httpClient.PostAsync(TokenUrl, content);
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Twitch auth {(int)response.StatusCode} {response.StatusCode}: {json}");
            var tokenData = JsonSerializer.Deserialize<TokenResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _accessToken = tokenData?.Access_token;
            _tokenExpiry = DateTime.UtcNow.AddSeconds((tokenData?.Expires_in ?? 3600) - 60);
        }

        // Raw JSON DTOs matching the IGDB response shape
        private class TokenResponse
        {
            public string? Access_token { get; set; }
            public int? Expires_in { get; set; }
            public string? Token_type { get; set; }
        }

        private class IgdbRawGame
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public List<IgdbRawGenre>? Genres { get; set; }
            public long? First_release_date { get; set; }
            public IgdbRawCover? Cover { get; set; }
            public string? Summary { get; set; }
        }

        private class IgdbRawGenre
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        private class IgdbRawCover
        {
            public int Id { get; set; }
            public string? Image_id { get; set; }
        }
    }
}
