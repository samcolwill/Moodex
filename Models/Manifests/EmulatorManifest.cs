using System.Text.Json.Serialization;

namespace Moodex.Models.Manifests
{
    public class EmulatorManifest
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("guid")] public string Guid { get; set; } = string.Empty;
        [JsonPropertyName("emulatedConsoleIds")] public List<string> EmulatedConsoleIds { get; set; } = new List<string>();
        [JsonPropertyName("executablePath")] public string ExecutablePath { get; set; } = string.Empty;
        [JsonPropertyName("defaultArguments")] public string DefaultArguments { get; set; } = string.Empty;
    }
}


