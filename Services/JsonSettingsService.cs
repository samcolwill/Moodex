using System;
using System.IO;
using System.Text.Json;

using Moodex.Configuration;
using Moodex.Models;

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
            var model = JsonSerializer.Deserialize<SettingsModel>(json)
                         ?? throw new InvalidOperationException("Couldn’t parse settings JSON");

            // Overlay genres/consoles from the active library's .moodex_library if present
            TryReadLibraryManifestInto(model);
            return model;
        }

        public void Save(SettingsModel model)
        {
            // Serialize with indentation for readability
            var json = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsFilePath, json);

            // Persist genres/consoles into the active library
            TryWriteLibraryManifestFrom(model);
        }

        private static void TryReadLibraryManifestInto(SettingsModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.ActiveLibraryPath)) return;
                var path = Path.Combine(model.ActiveLibraryPath, ".moodex_library");
                if (!File.Exists(path)) return;

                var json = File.ReadAllText(path);
                var lib = JsonSerializer.Deserialize<LibraryManifestDto>(json);
                if (lib == null) return;

                if (lib.Genres != null && lib.Genres.Count > 0)
                    model.Genres = lib.Genres.OrderBy(g => g, StringComparer.OrdinalIgnoreCase).ToList();
                if (lib.Consoles != null && lib.Consoles.Count > 0)
                    model.Consoles = lib.Consoles.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch { /* ignore invalid or missing manifests */ }
        }

        private static void TryWriteLibraryManifestFrom(SettingsModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.ActiveLibraryPath)) return;
                Directory.CreateDirectory(model.ActiveLibraryPath);
                var path = Path.Combine(model.ActiveLibraryPath, ".moodex_library");
                var dto = new LibraryManifestDto
                {
                    Genres = model.Genres.OrderBy(g => g, StringComparer.OrdinalIgnoreCase).ToList(),
                    Consoles = model.Consoles.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList()
                };
                var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { /* ignore write errors to avoid blocking settings save */ }
        }

        private sealed class LibraryManifestDto
        {
            public List<string> Genres { get; set; } = new List<string>();
            public List<ConsoleInfo> Consoles { get; set; } = new List<ConsoleInfo>();
        }
    }

}
