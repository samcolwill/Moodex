using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SamsGameLauncher.Models
{
    public class GameJsonConverter : JsonConverter<GameBase>
    {
        public override GameBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument document = JsonDocument.ParseValue(ref reader))
            {
                if (document.RootElement.TryGetProperty("GameType", out JsonElement gameTypeElement))
                {
                    string gameTypeStr = gameTypeElement.GetString();
                    GameBase game = gameTypeStr switch
                    {
                        "Emulated" => document.RootElement.Deserialize<EmulatedGame>(options),
                        "Native" => document.RootElement.Deserialize<NativeGame>(options),
                        "FolderBased" => document.RootElement.Deserialize<FolderBasedGame>(options),
                        _ => throw new JsonException("Unknown game type: " + gameTypeStr)
                    };

                    return game;
                }
                else
                {
                    throw new JsonException("Missing 'GameType' property.");
                }
            }
        }

        public override void Write(Utf8JsonWriter writer, GameBase value, JsonSerializerOptions options)
        {
            // Serialize the value according to its runtime type.
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }
    }
}
