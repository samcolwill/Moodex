using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Moodex.Models;
using Moodex.Services;
using Moodex.Services.Igdb;
using System.IO;
using System.Text.Json;
using Moodex.Models.Manifests;

namespace Moodex.ViewModels
{
    public class EditGameWindowViewModel : BaseViewModel
    {
        private readonly GameInfo _originalGame;
        private readonly IIgdbService _igdbService;

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
        public IRelayCommand OpenGameFilesCommand { get; }
        public IRelayCommand AddGameCoverCommand { get; }
        public IRelayCommand ConfirmGameCoverCommand { get; }
        public IAsyncRelayCommand FetchGameDataCommand { get; }
        public IAsyncRelayCommand FetchGameCoverCommand { get; }
        private string? _pendingCoverPath;

        // ───────────── IGDB state ─────────────
        public bool IsIgdbEnabled => _igdbService.IsEnabled;

        private string _igdbStatus = string.Empty;
        public string IgdbStatus
        {
            get => _igdbStatus;
            private set { _igdbStatus = value; RaisePropertyChanged(); }
        }

        // ───────────── ctor ─────────────
        [SupportedOSPlatform("windows")]
        public EditGameWindowViewModel(GameInfo gameToEdit,
                                       ISettingsService settingsService,
                                       IIgdbService igdbService)
        {
            _originalGame = gameToEdit;
            _igdbService = igdbService;

            var settings = settingsService.Load();
            Consoles = settings.Consoles;
            Genres = settings.Genres;

            // wire up commands
            BrowseCommand = new RelayCommand<Window?>(ExecuteBrowse);
            SaveCommand = new RelayCommand<Window?>(ExecuteSave, _ => CanSave());
            CancelCommand = new RelayCommand<Window?>(w => w?.Close());
            OpenGameFilesCommand = new RelayCommand(ExecuteOpenGameFiles);
            AddGameCoverCommand = new RelayCommand(ExecuteAddGameCover);
            ConfirmGameCoverCommand = new RelayCommand(ExecuteConfirmGameCover, () => !string.IsNullOrWhiteSpace(_pendingCoverPath));
            FetchGameDataCommand = new AsyncRelayCommand(ExecuteFetchGameData, () => IsIgdbEnabled && !string.IsNullOrWhiteSpace(Name));
            FetchGameCoverCommand = new AsyncRelayCommand(ExecuteFetchGameCover, () => IsIgdbEnabled && !string.IsNullOrWhiteSpace(Name));

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
            && (!string.IsNullOrWhiteSpace(FileSystemPath) || _originalGame.IsSteamGame)
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
                    // Store relative path under data\ when possible; otherwise store absolute
                    var dataDir = Path.Combine(root, "data");
                    string launchTargetToSave = FileSystemPath;
                    try
                    {
                        var dataDirTrim = dataDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        if (!string.IsNullOrWhiteSpace(FileSystemPath)
                            && FileSystemPath.StartsWith(dataDirTrim, StringComparison.OrdinalIgnoreCase))
                        {
                            launchTargetToSave = Path.GetRelativePath(dataDir, FileSystemPath);
                        }
                    }
                    catch { /* fallback to absolute */ }
                    man.LaunchTarget = launchTargetToSave;
                    man.LaunchType = IsFolderBasedConsole ? "folder" : "file";
                    man.Genres = SelectedGenres.ToList();
                    man.ReleaseDateTime = ReleaseDate;

                    File.WriteAllText(manifestPath, JsonSerializer.Serialize(man, new JsonSerializerOptions { WriteIndented = true }));

                    // Update runtime copy based on what we saved
                    _originalGame.LaunchTarget = man.LaunchTarget;
                    if (!string.IsNullOrWhiteSpace(_originalGame.LaunchTarget))
                    {
                        _originalGame.FileSystemPath = Path.IsPathRooted(_originalGame.LaunchTarget)
                            ? _originalGame.LaunchTarget
                            : Path.Combine(dataDir, _originalGame.LaunchTarget);
                    }
                }
                catch { /* ignore manifest write errors for now */ }
            }

            if (owner != null)
            {
                owner.DialogResult = true;
                owner.Close();
            }
        }

        private void ExecuteOpenGameFiles()
        {
            try
            {
                var root = _originalGame.GameRootPath;
                if (string.IsNullOrEmpty(root)) return;
                var dataDir = System.IO.Path.Combine(root, "data");
                System.IO.Directory.CreateDirectory(dataDir);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"\"{dataDir}\"") { UseShellExecute = true });
            }
            catch { }
        }

        private void ExecuteAddGameCover()
        {
            try
            {
                var root = _originalGame.GameRootPath;
                if (string.IsNullOrEmpty(root)) return;
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*"
                };
                dlg.InitialDirectory = root;
                if (dlg.ShowDialog() == true)
                {
                    _pendingCoverPath = dlg.FileName;
                    // Immediately apply the new cover to the library
                    try
                    {
                        var ext = System.IO.Path.GetExtension(_pendingCoverPath) ?? ".png";
                        // Remove any existing cover.* files to avoid stale images
                        foreach (var e in new[] { ".png", ".jpg", ".jpeg" })
                        {
                            var old = System.IO.Path.Combine(root, "cover" + e);
                            try { if (System.IO.File.Exists(old)) System.IO.File.Delete(old); } catch { }
                        }
                        var dest = System.IO.Path.Combine(root, "cover" + ext);
                        System.IO.File.Copy(_pendingCoverPath, dest, overwrite: true);
                        // Invalidate image converter cache for this path
                        Moodex.Converters.FreezingBitmapConverter.Invalidate(dest);
                        // Notify UI to refresh cover image
                        _originalGame.NotifyCoverChanged();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Failed to set cover image:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch { }
        }

        private void ExecuteConfirmGameCover()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_pendingCoverPath)) return;
                var root = _originalGame.GameRootPath;
                if (string.IsNullOrEmpty(root)) return;
                var ext = System.IO.Path.GetExtension(_pendingCoverPath);
                var dest = System.IO.Path.Combine(root, "cover" + ext);
                System.IO.File.Copy(_pendingCoverPath, dest, overwrite: true);
            }
            catch { }
        }

        // ───────────── IGDB command bodies ─────────────
        private async Task ExecuteFetchGameData()
        {
            if (string.IsNullOrWhiteSpace(Name)) return;

            IgdbStatus = "Searching IGDB...";
            try
            {
                var results = await _igdbService.SearchGameAsync(Name);
                if (results.Count == 0)
                {
                    IgdbStatus = "No results found.";
                    return;
                }

                var match = results.FirstOrDefault(r =>
                    string.Equals(r.Name, Name, StringComparison.OrdinalIgnoreCase))
                    ?? results[0];

                var preview = new Views.Utilities.IgdbPreviewDataDialog(match);
                if (preview.ShowDialog() != true)
                {
                    IgdbStatus = "Cancelled.";
                    return;
                }

                if (match.Genres.Count > 0)
                {
                    SelectedGenres.Clear();
                    foreach (var g in match.Genres)
                        SelectedGenres.Add(g);
                }

                if (match.ReleaseDate.HasValue)
                    ReleaseDate = match.ReleaseDate.Value;

                IgdbStatus = $"Applied data from \"{match.Name}\".";
            }
            catch (Exception ex)
            {
                IgdbStatus = "Error (see popup)";
                System.Windows.MessageBox.Show(ex.Message, "IGDB Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteFetchGameCover()
        {
            if (string.IsNullOrWhiteSpace(Name)) return;

            var root = _originalGame.GameRootPath;
            if (string.IsNullOrEmpty(root))
            {
                IgdbStatus = "No game folder found.";
                return;
            }

            IgdbStatus = "Fetching cover from IGDB...";
            try
            {
                var results = await _igdbService.SearchGameAsync(Name);
                var match = results.FirstOrDefault(r =>
                    string.Equals(r.Name, Name, StringComparison.OrdinalIgnoreCase))
                    ?? results.FirstOrDefault();

                if (match?.CoverImageId == null)
                {
                    IgdbStatus = "No cover found on IGDB.";
                    return;
                }

                var coverBytes = await _igdbService.DownloadCoverAsync(match.CoverImageId);
                if (coverBytes == null || coverBytes.Length == 0)
                {
                    IgdbStatus = "Failed to download cover.";
                    return;
                }

                var preview = new Views.Utilities.IgdbPreviewCoverDialog(coverBytes);
                if (preview.ShowDialog() != true)
                {
                    IgdbStatus = "Cancelled.";
                    return;
                }

                foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
                {
                    var old = Path.Combine(root, "cover" + ext);
                    try { if (File.Exists(old)) File.Delete(old); } catch { }
                }

                var dest = Path.Combine(root, "cover.jpg");
                File.WriteAllBytes(dest, coverBytes);
                Moodex.Converters.FreezingBitmapConverter.Invalidate(dest);
                _originalGame.NotifyCoverChanged();

                IgdbStatus = "Cover updated.";
            }
            catch (Exception ex)
            {
                IgdbStatus = "Error (see popup)";
                System.Windows.MessageBox.Show(ex.Message, "IGDB Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

