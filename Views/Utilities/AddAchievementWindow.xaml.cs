using System.IO;
using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Moodex.Models;
using System.Text.Json;
using Moodex.Models.Manifests;

namespace Moodex.Views.Utilities
{
    public partial class AddAchievementWindow : Window
    {
        public class VM : INotifyPropertyChanged
        {
            private readonly GameInfo _game;
            public string Name { get; set; } = "";
            public string ImagePath { get; set; } = "";
            public IRelayCommand BrowseCommand { get; }
            public IRelayCommand AddCommand { get; }
            public VM(GameInfo game, Window owner)
            {
                _game = game;
                BrowseCommand = new RelayCommand(() =>
                {
                    var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All Files|*.*" };
                    if (dlg.ShowDialog(owner) == true)
                    {
                        ImagePath = dlg.FileName;
                        OnPropertyChanged(nameof(ImagePath));
                    }
                });
                AddCommand = new RelayCommand(() =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(_game.GameRootPath)) return;
                        var folder = System.IO.Path.Combine(_game.GameRootPath, "achievements");
                        Directory.CreateDirectory(folder);
                        var ext = System.IO.Path.GetExtension(ImagePath);
                        var safeName = string.IsNullOrWhiteSpace(Name) ? System.IO.Path.GetFileNameWithoutExtension(ImagePath) : Name;
                        foreach (var c in Path.GetInvalidFileNameChars()) safeName = safeName.Replace(c, '_');
                        var dest = System.IO.Path.Combine(folder, $"{safeName}{ext}");
                        File.Copy(ImagePath, dest, overwrite: true);
                        // update manifest flag
                        var manPath = System.IO.Path.Combine(_game.GameRootPath, ".moodex_game");
                        GameManifest man;
                        if (File.Exists(manPath))
                        {
                            var json = File.ReadAllText(manPath);
                            man = JsonSerializer.Deserialize<GameManifest>(json) ?? new GameManifest();
                        }
                        else
                        {
                            man = new GameManifest { Name = _game.Name, Guid = _game.GameGuid ?? System.Guid.NewGuid().ToString(), ConsoleId = _game.ConsoleId };
                        }
                        man.HasAchievements = true;
                        File.WriteAllText(manPath, JsonSerializer.Serialize(man, new JsonSerializerOptions { WriteIndented = true }));
                        _game.HasAchievements = true;
                        owner.DialogResult = true;
                        owner.Close();
                    }
                    catch
                    {
                        // ignore errors for now
                    }
                });
            }
            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public AddAchievementWindow(GameInfo game)
        {
            InitializeComponent();
            DataContext = new VM(game, this);
        }
    }
}


