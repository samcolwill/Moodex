using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Moodex.Models;

namespace Moodex.Views.Utilities
{
    public partial class ManageAchievementsWindow : Window
    {
        public class VM : INotifyPropertyChanged
        {
            private readonly GameInfo _game;
            public ObservableCollection<string> Items { get; } = new();
            private int _selectedIndex;
            public int SelectedIndex
            {
                get => _selectedIndex;
                set { if (_selectedIndex != value) { _selectedIndex = value; OnPropertyChanged(nameof(SelectedIndex)); } }
            }
            public IRelayCommand MoveUpCommand { get; }
            public IRelayCommand MoveDownCommand { get; }
            public IRelayCommand DeleteCommand { get; }
            public IRelayCommand SaveCommand { get; }

            public VM(GameInfo game, Window owner)
            {
                _game = game;
                Load();
                MoveUpCommand = new RelayCommand(() =>
                {
                    if (SelectedIndex <= 0) return;
                    var i = SelectedIndex;
                    var item = Items[i];
                    Items.RemoveAt(i);
                    Items.Insert(i - 1, item);
                    SelectedIndex = i - 1;
                });
                MoveDownCommand = new RelayCommand(() =>
                {
                    if (SelectedIndex < 0 || SelectedIndex >= Items.Count - 1) return;
                    var i = SelectedIndex;
                    var item = Items[i];
                    Items.RemoveAt(i);
                    Items.Insert(i + 1, item);
                    SelectedIndex = i + 1;
                });
                DeleteCommand = new RelayCommand(() =>
                {
                    if (SelectedIndex < 0) return;
                    var name = Items[SelectedIndex];
                    var folder = GetAchievementsFolder();
                    if (folder == null) return;
                    var paths = Directory.GetFiles(folder)
                        .Where(f => string.Equals(StripPrefix(Path.GetFileNameWithoutExtension(f)), name, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    foreach (var p in paths)
                    {
                        try { File.Delete(p); } catch { }
                    }
                    Items.RemoveAt(SelectedIndex);
                    SelectedIndex = Math.Min(SelectedIndex, Items.Count - 1);
                    UpdateHasAchievementsFlag();
                });
                SaveCommand = new RelayCommand(() =>
                {
                    var folder = GetAchievementsFolder();
                    if (folder == null) return;
                    var exts = new HashSet<string>(new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" }, StringComparer.OrdinalIgnoreCase);
                    var allFiles = Directory.GetFiles(folder).Where(f => exts.Contains(Path.GetExtension(f))).ToList();

                    // Move ALL files to temp (not just first match per base)
                    var tempFiles = new List<(string BaseName, string TempPath, string Ext)>();
                    foreach (var f in allFiles)
                    {
                        var baseName = StripPrefix(Path.GetFileNameWithoutExtension(f));
                        var tmp = Path.Combine(folder, Guid.NewGuid().ToString("N") + Path.GetExtension(f));
                        try { File.Move(f, tmp); tempFiles.Add((baseName, tmp, Path.GetExtension(f))); } catch { }
                    }

                    // Build final ordering: first by Items order (one per base name if available), then any leftovers
                    var finals = new List<(string BaseName, string TempPath, string Ext)>();
                    var used = new HashSet<int>();
                    foreach (var baseName in Items)
                    {
                        var idx = tempFiles.FindIndex(t => string.Equals(t.BaseName, baseName, StringComparison.OrdinalIgnoreCase) && !used.Contains(tempFiles.IndexOf(t)));
                        if (idx >= 0 && !used.Contains(idx))
                        {
                            finals.Add(tempFiles[idx]);
                            used.Add(idx);
                        }
                    }
                    for (int i = 0; i < tempFiles.Count; i++)
                    {
                        if (!used.Contains(i)) finals.Add(tempFiles[i]);
                    }

                    // Move from temp to final numbered names
                    for (int i = 0; i < finals.Count; i++)
                    {
                        var f = finals[i];
                        var final = Path.Combine(folder, $"{(i + 1).ToString().PadLeft(3, '0')}_{f.BaseName}{f.Ext}");
                        try { File.Move(f.TempPath, final); } catch { }
                    }

                    UpdateHasAchievementsFlag();
                    owner.DialogResult = true;
                    owner.Close();
                });
            }

            private void Load()
            {
                Items.Clear();
                var folder = GetAchievementsFolder();
                if (folder == null) return;
                var list = Directory.GetFiles(folder)
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .OrderBy(f => ParseOrder(f))
                    .ThenBy(f => StripPrefix(f))
                    .Select(f => StripPrefix(f))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
                foreach (var n in list) Items.Add(n);
            }

            private string? GetAchievementsFolder()
            {
                if (string.IsNullOrEmpty(_game.GameRootPath)) return null;
                var folder = Path.Combine(_game.GameRootPath, "achievements");
                Directory.CreateDirectory(folder);
                return folder;
            }

            private static int ParseOrder(string baseName)
            {
                if (baseName.Length >= 4 && char.IsDigit(baseName[0]) && char.IsDigit(baseName[1]) && char.IsDigit(baseName[2]) && baseName[3] == '_')
                {
                    if (int.TryParse(baseName.Substring(0, 3), out var n)) return n;
                }
                return int.MaxValue;
            }
            private static string StripPrefix(string baseName)
            {
                if (baseName.Length >= 4 && char.IsDigit(baseName[0]) && char.IsDigit(baseName[1]) && char.IsDigit(baseName[2]) && baseName[3] == '_')
                {
                    return baseName.Substring(4);
                }
                return baseName;
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            private void UpdateHasAchievementsFlag()
            {
                _game.HasAchievements = Items.Count > 0;
            }
        }

        public ManageAchievementsWindow(GameInfo game)
        {
            InitializeComponent();
            DataContext = new VM(game, this);
        }
    }
}


