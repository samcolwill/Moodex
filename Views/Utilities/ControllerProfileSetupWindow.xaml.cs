using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Moodex.Models;
using Moodex.Models.Manifests;
using Moodex.Utilities;
using System.Xml.Linq;

namespace Moodex.Views.Utilities
{
    public partial class ControllerProfileSetupWindow : Window
    {
        private readonly GameInfo _game;
        private Process? _launchedDs4;
        private string? _profilesXmlBackup;
        private string? _profilesXmlPath;

        public ControllerProfileSetupWindow(GameInfo game)
        {
            _game = game;
            InitializeComponent();
        }

        private static string GetDs4BaseDir()
        {
            var baseDir = Path.Combine(AppContext.BaseDirectory, "External Tools", "DS4Windows");
            return baseDir;
        }

        private static string? FindDs4Exe()
        {
            var baseDir = GetDs4BaseDir();
            if (!Directory.Exists(baseDir)) return null;
            return Directory.GetFiles(baseDir, "DS4Windows.exe", SearchOption.AllDirectories).FirstOrDefault();
        }

        private static string GetDs4DefaultProfilePath()
        {
            var baseDir = GetDs4BaseDir();
            var profilesDir = Path.Combine(baseDir, "Profiles");
            Directory.CreateDirectory(profilesDir);
            return Path.Combine(profilesDir, "Default.xml");
        }

        private static string GetDs4ProfilesXmlPath()
        {
            var baseDir = GetDs4BaseDir();
            return Path.Combine(baseDir, "Profiles.xml");
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // If a per-game profile exists, load it into DS4Windows Default.xml before launching
                if (!string.IsNullOrEmpty(_game.GameRootPath))
                {
                    var perGame = Path.Combine(_game.GameRootPath, "input", "ds4windows_controller_profile.xml");
                    if (File.Exists(perGame))
                    {
                        var defaultXml = GetDs4DefaultProfilePath();
                        Directory.CreateDirectory(Path.GetDirectoryName(defaultXml)!);
                        File.Copy(perGame, defaultXml, overwrite: true);
                    }
                }

                // Ensure DS4Windows starts with a visible window (override startMinimized)
                try
                {
                    _profilesXmlPath = GetDs4ProfilesXmlPath();
                    if (File.Exists(_profilesXmlPath))
                    {
                        _profilesXmlBackup = File.ReadAllText(_profilesXmlPath);
                        var doc = XDocument.Load(_profilesXmlPath);
                        var root = doc.Root;
                        var startMin = root?.Element("startMinimized");
                        if (startMin != null)
                        {
                            startMin.Value = "False";
                            doc.Save(_profilesXmlPath);
                        }
                    }
                }
                catch { }

                var exe = FindDs4Exe();
                if (exe == null)
                {
                    System.Windows.MessageBox.Show("DS4Windows is not installed.", "DS4Windows", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _launchedDs4 = Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to launch DS4Windows:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Close the DS4Windows instance we launched (if any)
                if (_launchedDs4 != null && !_launchedDs4.HasExited)
                {
                    try
                    {
                        _launchedDs4.CloseMainWindow();
                        if (!_launchedDs4.WaitForExit(2000)) _launchedDs4.Kill();
                    }
                    catch { }
                }

                // Restore Profiles.xml (startMinimized back to previous setting)
                try
                {
                    if (!string.IsNullOrEmpty(_profilesXmlPath) && _profilesXmlBackup != null)
                    {
                        File.WriteAllText(_profilesXmlPath, _profilesXmlBackup);
                    }
                }
                catch { }

                // Persist Default.xml into the game's input folder
                if (string.IsNullOrEmpty(_game.GameRootPath))
                {
                    System.Windows.MessageBox.Show("Game root path is not set; cannot save controller profile.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var defaultXml = GetDs4DefaultProfilePath();
                if (!File.Exists(defaultXml))
                {
                    System.Windows.MessageBox.Show("DS4Windows Default.xml not found; nothing to save.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var inputDir = Path.Combine(_game.GameRootPath, "input");
                Directory.CreateDirectory(inputDir);
                var dest = Path.Combine(inputDir, "ds4windows_controller_profile.xml");
                File.Copy(defaultXml, dest, overwrite: true);

                // Update in-memory and manifest
                _game.ControllerProfileConfigured = true;
                UpdateGameManifest(man => man.ControllerProfileConfigured = true);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save controller profile:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void UpdateGameManifest(Action<GameManifest> update)
        {
            try
            {
                if (string.IsNullOrEmpty(_game.GameRootPath)) return;
                var path = Path.Combine(_game.GameRootPath, ".moodex_game");
                GameManifest man;
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    man = System.Text.Json.JsonSerializer.Deserialize<GameManifest>(json) ?? new GameManifest();
                }
                else
                {
                    man = new GameManifest { Name = _game.Name, Guid = _game.GameGuid ?? Guid.NewGuid().ToString(), ConsoleId = _game.ConsoleId };
                }
                update(man);
                File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(man, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
    }
}


