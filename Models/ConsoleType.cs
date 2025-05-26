using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace SamsGameLauncher.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ConsoleType
    {
        [Description("None")]
        None,
        [Description("PC")]
        PC,
        [Description("Playstation 1")]
        Playstation1,
        [Description("Playstation 2")]
        Playstation2,
        [Description("Playstation 3")]
        Playstation3,
        [Description("Xbox")]
        Xbox,
        [Description("Game Boy Advance")]
        GameBoyAdvance,
        [Description("Nintendo Switch")]
        NintendoSwitch
    }
}
