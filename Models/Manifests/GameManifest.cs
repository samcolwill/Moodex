using System.Text.Json.Serialization;

namespace Moodex.Models.Manifests
{
    public class GameManifest
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("guid")] public string Guid { get; set; } = string.Empty;
        [JsonPropertyName("addedDateTime")] public DateTime? AddedDateTime { get; set; }
        [JsonPropertyName("consoleId")] public string ConsoleId { get; set; } = string.Empty;
        [JsonPropertyName("launchTarget")] public string LaunchTarget { get; set; } = string.Empty;
        [JsonPropertyName("launchType")] public string LaunchType { get; set; } = "file"; // file|folder
        [JsonPropertyName("genre")] public List<string> Genres { get; set; } = new();
        [JsonPropertyName("releaseDateTime")] public DateTime? ReleaseDateTime { get; set; }
        [JsonPropertyName("lastPlayed")] public DateTime? LastPlayed { get; set; }
        [JsonPropertyName("playTimeMinutes")] public int PlayTimeMinutes { get; set; }
        [JsonPropertyName("archived")] public bool Archived { get; set; }
        [JsonPropertyName("archivedDateTime")] public DateTime? ArchivedDateTime { get; set; }
        [JsonPropertyName("completed")] public bool Completed { get; set; }
    }
}


