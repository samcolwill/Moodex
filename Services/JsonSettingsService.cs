using System;
using System.IO;
using System.Text.Json;

using SamsGameLauncher.Configuration;

namespace SamsGameLauncher.Services
{
    public class JsonSettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        private readonly string _defaultSettingsPath;

        public JsonSettingsService()
        {
            // 1) All portable: exe folder
            var exeDir = AppContext.BaseDirectory;
            _settingsFilePath = Path.Combine(exeDir, "settings.json");

            // 2) Defaults reside in Configuration\default_settings.json under exe folder
            _defaultSettingsPath = Path.Combine(exeDir, "Configuration", "default_settings.json");
        }

        public SettingsModel Load()
        {
            // If no settings.json yet, try to seed it
            if (!File.Exists(_settingsFilePath))
            {
                if (File.Exists(_defaultSettingsPath))
                {
                    File.Copy(_defaultSettingsPath, _settingsFilePath);
                }
                else
                {
                    // No default file? Create one from hard‑coded defaults
                    var empty = new SettingsModel();
                    var starter = JsonSerializer.Serialize(empty,
                        new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_settingsFilePath, starter);
                }
            }

            // Read & deserialize
            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<SettingsModel>(json)
                   ?? throw new InvalidOperationException("Couldn’t parse settings JSON");
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