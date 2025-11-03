using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Moodex.Models;
using Moodex.Services;
using System.IO;
using System.Text.Json;
using Moodex.Models.Manifests;

namespace Moodex.ViewModels
{
    public class EditGameWindowViewModel : BaseViewModel
    {
        private readonly GameInfo _originalGame;

        // ───────────── backing fields ─────────────
        private string _name = "";
        private string _fileSystemPath = "";
        private string _consoleId = "";
        private string _genre = ""; // legacy single-genre field, not bound
        private DateTime _releaseDate = DateTime.Today;
        private string _genreToAdd = "";
        private string? _selectedGenreInList;

        // which console-IDs should show the “folder picker”?
        private static readonly HashSet<string> _folderConsoleIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "Playstation3"
        };

        private bool IsFolderBasedConsole => !string.IsNullOrEmpty(ConsoleId)
            && _folderConsoleIds.Contains(ConsoleId);

        // ───────────── dropdown sources ─────────────
        public IReadOnlyList<ConsoleInfo> Consoles { get; }
        public IReadOnlyList<string> Genres { get; }
        public ObservableCollection<string> SelectedGenres { get; } = new();

        // ───────────── bindable props ─────────────
        public string Name
        {
            get => _name;
            set { _name = value; RaisePropertyChanged(); SaveCommand.NotifyCanExecuteChanged(); }
        }

        public string FileSystemPath
        {
            get => _fileSystemPath;
            set { _fileSystemPath = value; RaisePropertyChanged(); SaveCommand.NotifyCanExecuteChanged(); }
        }

        public string ConsoleId
        {
            get => _consoleId;
            set { _consoleId = value; RaisePropertyChanged(); }
        }

        public string Genre
        {
            get => _genre;
            set { _genre = value; RaisePropertyChanged(); }
        }

        public string GenreToAdd
        {
            get => _genreToAdd;
            set { _genreToAdd = value; RaisePropertyChanged(); }
        }

        public string? SelectedGenreInList
        {
            get => _selectedGenreInList;
            set { _selectedGenreInList = value; RaisePropertyChanged(); }
        }

        public DateTime ReleaseDate
        {
            get => _releaseDate;
            set { _releaseDate = value; RaisePropertyChanged(); }
        }

        // ───────────── commands (Toolkit) ─────────────
        public IRelayCommand BrowseCommand { get; }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand AddGenreCommand { get; }
        public IRelayCommand<string?> RemoveGenreCommand { get; }
        public IRelayCommand CancelCommand { get; }

        // ───────────── ctor ─────────────
        [SupportedOSPlatform("windows")]
        public EditGameWindowViewModel(GameInfo gameToEdit,
                                       ISettingsService settingsService)
        {
            _originalGame = gameToEdit;

            var settings = settingsService.Load();
            Consoles = settings.Consoles;
            Genres = settings.Genres;

            // wire up commands
            BrowseCommand = new RelayCommand<Window?>(ExecuteBrowse);
            SaveCommand = new RelayCommand<Window?>(ExecuteSave, _ => CanSave());
            CancelCommand = new RelayCommand<Window?>(w => w?.Close());

            // seed the form
            Name = gameToEdit.Name;
            FileSystemPath = gameToEdit.FileSystemPath;
            ConsoleId = gameToEdit.ConsoleId;
            ReleaseDate = gameToEdit.ReleaseDate;

            // load genres from manifest (fallback to GameInfo.Genre split)
            try
            {
                var root = gameToEdit.GameRootPath;
                if (!string.IsNullOrEmpty(root))
                {
                    var path = Path.Combine(root, ".moodex_game");
                    if (File.Exists(path))
                    {
                        var json = File.ReadAllText(path);
                        var man = JsonSerializer.Deserialize<GameManifest>(json);
                        if (man?.Genres != null)
                        {
                            foreach (var g in man.Genres)
                                if (!string.IsNullOrWhiteSpace(g)) SelectedGenres.Add(g);
                        }
                    }
                }
                if (SelectedGenres.Count == 0 && !string.IsNullOrWhiteSpace(gameToEdit.Genre))
                {
                    foreach (var g in gameToEdit.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        SelectedGenres.Add(g);
                }
            }
            catch { }

            AddGenreCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrWhiteSpace(GenreToAdd) && !SelectedGenres.Any(g => string.Equals(g, GenreToAdd, StringComparison.OrdinalIgnoreCase)))
                {
                    SelectedGenres.Add(GenreToAdd);
                }
            });
            RemoveGenreCommand = new RelayCommand<string?>(g =>
            {
                if (!string.IsNullOrWhiteSpace(g))
                {
                    SelectedGenres.Remove(g);
                }
            });
        }

        // ───────────── command bodies ─────────────
        [SupportedOSPlatform("windows")]
        private void ExecuteBrowse(Window? owner)
        {
            if (IsFolderBasedConsole)
            {
                var fb = new System.Windows.Forms.FolderBrowserDialog();
                if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    FileSystemPath = fb.SelectedPath;
            }
            else
            {
                var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "All Files (*.*)|*.*" };
                if (ofd.ShowDialog(owner) == true)
                    FileSystemPath = ofd.FileName;
            }
        }

        private bool CanSave()
            => !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(FileSystemPath)
            && !string.IsNullOrWhiteSpace(ConsoleId);

        private void ExecuteSave(Window? owner)
        {
            // copy back into the model
            _originalGame.Name = Name;
            _originalGame.FileSystemPath = FileSystemPath;
            _originalGame.ConsoleId = ConsoleId;
            _originalGame.Genre = SelectedGenres.Count > 0 ? string.Join(", ", SelectedGenres) : string.Empty;
            _originalGame.ReleaseDate = ReleaseDate;

            // Update manifest if present
            var root = _originalGame.GameRootPath;
            if (!string.IsNullOrEmpty(root))
            {
                try
                {
                    var manifestPath = Path.Combine(root, ".moodex_game");
                    GameManifest man;
                    if (File.Exists(manifestPath))
                    {
                        var json = File.ReadAllText(manifestPath);
                        man = JsonSerializer.Deserialize<GameManifest>(json) ?? new GameManifest();
                    }
                    else
                    {
                        man = new GameManifest();
                    }

                    man.Name = Name;
                    man.ConsoleId = ConsoleId;
                    man.LaunchTarget = Path.GetFileName(FileSystemPath);
                    man.LaunchType = IsFolderBasedConsole ? "folder" : "file";
                    man.Genres = SelectedGenres.ToList();
                    man.ReleaseDateTime = ReleaseDate;

                    File.WriteAllText(manifestPath, JsonSerializer.Serialize(man, new JsonSerializerOptions { WriteIndented = true }));
                }
                catch { /* ignore manifest write errors for now */ }
            }

            if (owner != null)
            {
                owner.DialogResult = true;
                owner.Close();
            }
        }
    }
}

