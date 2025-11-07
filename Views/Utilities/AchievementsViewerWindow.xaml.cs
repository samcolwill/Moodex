using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Moodex.Models;

namespace Moodex.Views.Utilities
{
    public partial class AchievementsViewerWindow : Window
    {
        public class VM : INotifyPropertyChanged
        {
            private readonly ObservableCollection<string> _images = new();
            private int _idx = 0;
            public string CurrentImagePath => _images.Count == 0 ? "" : _images[_idx];
            public string CurrentName => _images.Count == 0 ? "No achievements" : StripPrefix(System.IO.Path.GetFileNameWithoutExtension(CurrentImagePath));
            public string PositionText => _images.Count == 0 ? "0 / 0" : $"{_idx + 1} / {_images.Count}";
            public IRelayCommand NextCommand { get; }
            public IRelayCommand PrevCommand { get; }
            public void Load(string folder)
            {
                _images.Clear();
                if (Directory.Exists(folder))
                {
                    var exts = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" };
                    var files = Directory.GetFiles(folder)
                        .Where(f => exts.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant()))
                        .Select(f => new { Path = f, Order = ParseOrder(System.IO.Path.GetFileNameWithoutExtension(f)) })
                        .OrderBy(x => x.Order)
                        .ThenBy(x => StripPrefix(System.IO.Path.GetFileNameWithoutExtension(x.Path)))
                        .Select(x => x.Path);
                    foreach (var f in files) _images.Add(f);
                }
                if (_idx >= _images.Count) _idx = _images.Count > 0 ? _images.Count - 1 : 0;
                RaiseAll();
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
            private void RaiseAll()
            {
                OnPropertyChanged(nameof(CurrentImagePath));
                OnPropertyChanged(nameof(CurrentName));
                OnPropertyChanged(nameof(PositionText));
            }
            public VM()
            {
                NextCommand = new RelayCommand(() =>
                {
                    if (_images.Count == 0) return;
                    _idx = (_idx + 1) % _images.Count;
                    RaiseAll();
                });
                PrevCommand = new RelayCommand(() =>
                {
                    if (_images.Count == 0) return;
                    _idx = (_idx - 1 + _images.Count) % _images.Count;
                    RaiseAll();
                });
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public AchievementsViewerWindow(GameInfo game)
        {
            InitializeComponent();
            var vm = new VM();
            DataContext = vm;
            var folder = string.IsNullOrEmpty(game.GameRootPath) ? "" : System.IO.Path.Combine(game.GameRootPath, "achievements");
            vm.Load(folder);
        }
    }
}


