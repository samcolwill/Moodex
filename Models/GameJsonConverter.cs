using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SamsGameLauncher.Models
{
    public class GameJsonConverter : JsonConverter<GameBase>
    {
        public override GameBase Read(ref Utf8JsonReader reader,
                                      Type typeToConvert,
                                      JsonSerializerOptions options)
        {
            // Parse the incoming JSON into a DOM so we can inspect "GameType"
            using var document = JsonDocument.ParseValue(ref reader);

            // Pull out the GameType field (must exist)
            if (!document.RootElement.TryGetProperty("GameType", out var typeElem))
                throw new JsonException("Missing 'GameType' property.");

            // Convert it to a string, or error if null
            var typeStr = typeElem.GetString()
                       ?? throw new JsonException("'GameType' was null");

            // Based on that string, deserialize the element into the correct subtype
            return typeStr switch
            {
                "Emulated" => document.RootElement.Deserialize<EmulatedGame>(options)
                                    ?? throw new JsonException("Failed to deserialize EmulatedGame"),
                "Native" => document.RootElement.Deserialize<NativeGame>(options)
                                    ?? throw new JsonException("Failed to deserialize NativeGame"),
                "FolderBased" => document.RootElement.Deserialize<FolderBasedGame>(options)
                                    ?? throw new JsonException("Failed to deserialize FolderBasedGame"),
                _ => throw new JsonException($"Unknown GameType: {typeStr}")
            };
        }

        public override void Write(Utf8JsonWriter writer,
                                   GameBase value,
                                   JsonSerializerOptions options)
        {
            // Round‑trip the object as its actual runtime type
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }
    }
}
