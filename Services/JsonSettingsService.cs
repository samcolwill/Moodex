using System;
using System.IO;
using System.Text.Json;

using Moodex.Configuration;

namespace Moodex.Services
{
    public class JsonSettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;

        public JsonSettingsService()
        {
            var exeDir = AppContext.BaseDirectory;
            _settingsFilePath = Path.Combine(exeDir, "settings.json");
        }

        public SettingsModel Load()
        {
            // If no settings.json yet, create one from hard-coded defaults
            if (!File.Exists(_settingsFilePath))
            {
                var starter = JsonSerializer.Serialize(
                    new SettingsModel(),
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, starter);
            }

            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<SettingsModel>(json)
                   ?? throw new InvalidOperationException("Couldnâ€™t parse settings JSON");
        }

        public void Save(SettingsModel model)
        {
            // Serialize with indentation for readability
            var json = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsFilePath, json);
        }
    }

}
