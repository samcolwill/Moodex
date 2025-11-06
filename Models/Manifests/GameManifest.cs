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
        // Completion levels
        [JsonPropertyName("completionAnyPercent")] public bool CompletionAnyPercent { get; set; }
        [JsonPropertyName("completionMaxDifficulty")] public bool CompletionMaxDifficulty { get; set; }
        [JsonPropertyName("completionHundredPercent")] public bool CompletionHundredPercent { get; set; }

        // Input scripts section (can be null when no scripts configured)
        [JsonPropertyName("inputScripts")] public List<InputScriptEntry>? InputScripts { get; set; }

        // Per-game controller settings
        [JsonPropertyName("controllerEnabled")] public bool ControllerEnabled { get; set; } = false;
        [JsonPropertyName("controllerProfileConfigured")] public bool ControllerProfileConfigured { get; set; } = false;
    }

    public class InputScriptEntry
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}


