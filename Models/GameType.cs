using System.Text.Json.Serialization;

namespace SamsGameLauncher.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GameType
    {
        Emulated,
        Native,
        FolderBased
    }
}