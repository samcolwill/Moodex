using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Moodex.Services;
using Moodex.Models;
using CommunityToolkit.Mvvm.Input;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System.IO;
using System.Text.Json;
using Moodex.Models.Manifests;
using System.Diagnostics;

namespace Moodex.ViewModels
{
    public class AddGameWindowViewModel : BaseViewModel
    {
        // ───────────── backing fields ─────────────
        private string _name = "";
        private string _consoleId = "";
        private string _fileSystemPath = "";
        private string _genre = ""; // legacy single-genre field, not bound
        private DateTime _releaseDate = DateTime.Today;
        private string _genreToAdd = "";
        private string? _selectedGenreInList;

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

        // ───────────── form-bound properties ─────────────
        public string Name
        {
            get => _name;
            set { _name = value; RaisePropertyChanged(); SaveCommand.NotifyCanExecuteChanged(); CreateGameFolderAndOpenCommand?.NotifyCanExecuteChanged(); ConfirmGameFolderCommand?.NotifyCanExecuteChanged(); AddGameCoverCommand?.NotifyCanExecuteChanged(); ConfirmGameCoverCommand?.NotifyCanExecuteChanged(); }
        }

        public string ConsoleId
        {
            get => _consoleId;
            set
            {
                if (_consoleId == value) return;
                _consoleId = value;
                RaisePropertyChanged();

                // Keep inputs locked until user explicitly confirms
                UpdateFileSystemPathLabel();
                SaveCommand.NotifyCanExecuteChanged();
                CreateGameFolderAndOpenCommand?.NotifyCanExecuteChanged();
                ConfirmGameFolderCommand?.NotifyCanExecuteChanged();
                AddGameCoverCommand?.NotifyCanExecuteChanged();
                ConfirmGameCoverCommand?.NotifyCanExecuteChanged();
            }
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

        public string FileSystemPath
        {
            get => _fileSystemPath;
            set { _fileSystemPath = value; RaisePropertyChanged(); SaveCommand.NotifyCanExecuteChanged(); }
        }

        // ───────────── UI-state flags ─────────────
        private bool _isFileSystemPathEnabled;
        public bool IsFileSystemPathEnabled
        {
            get => _isFileSystemPathEnabled;
            private set { _isFileSystemPathEnabled = value; RaisePropertyChanged(); }
        }

        private bool _isGenreEnabled;
        public bool IsGenreEnabled
        {
            get => _isGenreEnabled;
            private set { _isGenreEnabled = value; RaisePropertyChanged(); }
        }

        private bool _isReleaseDateEnabled;
        public bool IsReleaseDateEnabled
        {
            get => _isReleaseDateEnabled;
            private set { _isReleaseDateEnabled = value; RaisePropertyChanged(); }
        }

        private bool _isBrowseEnabled;
        public bool IsBrowseEnabled
        {
            get => _isBrowseEnabled;
            private set { _isBrowseEnabled = value; RaisePropertyChanged(); }
        }

        public string GameFileLabelText { get; private set; }

        // ───────────── commands (Toolkit) ─────────────
        public IRelayCommand BrowseCommand { get; }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand AddGenreCommand { get; }
        public IRelayCommand<string?> RemoveGenreCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IRelayCommand CreateGameFolderAndOpenCommand { get; }
        public IRelayCommand ConfirmGameFolderCommand { get; }
        public IRelayCommand AddGameCoverCommand { get; }
        public IRelayCommand ConfirmGameCoverCommand { get; }

        // result object
        public GameInfo? NewGame { get; private set; }

        // ───────────── ctor ─────────────
        private readonly ISettingsService _settingsService;
        private bool _gameFilesInitiated;
        private bool _gameFilesConfirmed;
        private bool _coverConfirmed;
        private string? _pendingCoverPath;

        public AddGameWindowViewModel(
            ISettingsService settingsService)
        {
            _settingsService = settingsService;
            var settings = _settingsService.Load();
            Consoles = settings.Consoles;
            Genres = settings.Genres;

            GameFileLabelText = "Game File:";
            SetAllControlsEnabled(false);
            _gameFilesInitiated = false;
            _gameFilesConfirmed = false;
            _coverConfirmed = false;

            // Toolkit commands (strongly typed generic)
            BrowseCommand = new RelayCommand<Window?>(ExecuteBrowse);
            SaveCommand = new RelayCommand<Window?>(ExecuteSave, _ => CanSave());
            CancelCommand = new RelayCommand<Window?>(w => w?.Close());
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

            CreateGameFolderAndOpenCommand = new RelayCommand(ExecuteCreateGameFolderAndOpen, CanCreateGameFolderAndOpen);
            ConfirmGameFolderCommand = new RelayCommand(ExecuteConfirmGameFolder, CanConfirmGameFolder);
            AddGameCoverCommand = new RelayCommand(ExecuteAddGameCover, CanAddGameCover);
            ConfirmGameCoverCommand = new RelayCommand(ExecuteConfirmGameCover, CanConfirmGameCover);
        }

        // ───────────── helpers ─────────────
        private void SetAllControlsEnabled(bool enabled)
        {
            IsFileSystemPathEnabled = enabled;
            IsBrowseEnabled = enabled;
            IsGenreEnabled = enabled;
            IsReleaseDateEnabled = enabled;
        }

        private void UpdateFileSystemPathLabel()
        {
            if (ConsoleId.Equals("PC", StringComparison.OrdinalIgnoreCase))
            {
                GameFileLabelText = "Game Executable:";
            }
            else if (IsFolderBasedConsole)
            {
                GameFileLabelText = "Game Folder:";
            }
            else
            {
                GameFileLabelText = "Game File:";
            }

            RaisePropertyChanged(nameof(GameFileLabelText));
        }

        private bool CanCreateGameFolderAndOpen()
        {
            return !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(ConsoleId);
        }

        private void ExecuteCreateGameFolderAndOpen()
        {
            try
            {
                var settings = _settingsService.Load();
                var libraryRoot = settings.ActiveLibraryPath ?? "C:\\Moodex Library";
                var consoleDisplay = Utilities.ConsoleRegistry.GetDisplayName(ConsoleId) ?? ConsoleId;
                var gameRoot = Path.Combine(libraryRoot, "Games", consoleDisplay, Name);
                var dataDir = Path.Combine(gameRoot, "data");
                Directory.CreateDirectory(dataDir);

                // Create placeholder marker file with no extension
                var placeholder = Path.Combine(dataDir, "Add game files here");
                if (!File.Exists(placeholder)) File.WriteAllText(placeholder, string.Empty);

                // Open Explorer at data folder
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dataDir}\"") { UseShellExecute = true });

                // Enable the Confirm button for game files after initiating the step
                _gameFilesInitiated = true;
                ConfirmGameFolderCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to create game folder or open Explorer:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanConfirmGameFolder()
        {
            return _gameFilesInitiated;
        }

        private void ExecuteConfirmGameFolder()
        {
            _gameFilesConfirmed = true;
            RaisePropertyChanged(nameof(GameFilesConfirmed));
            // Remove placeholder marker file once the user confirmed files were added
            try
            {
                var settings = _settingsService.Load();
                var libraryRoot = settings.ActiveLibraryPath ?? "C:\\Moodex Library";
                var consoleDisplay = Utilities.ConsoleRegistry.GetDisplayName(ConsoleId) ?? ConsoleId;
                var gameRoot = Path.Combine(libraryRoot, "Games", consoleDisplay, Name);
                var dataDir = Path.Combine(gameRoot, "data");
                var placeholder = Path.Combine(dataDir, "Add game files here");
                if (File.Exists(placeholder)) File.Delete(placeholder);
            }
            catch { }
            UpdateInputsEnabled();
            AddGameCoverCommand?.NotifyCanExecuteChanged();
        }

        private bool CanAddGameCover()
        {
            return !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(ConsoleId);
        }

        private void ExecuteAddGameCover()
        {
            try
            {
                var settings = _settingsService.Load();
                var libraryRoot = settings.ActiveLibraryPath ?? "C:\\Moodex Library";
                var consoleDisplay = Utilities.ConsoleRegistry.GetDisplayName(ConsoleId) ?? ConsoleId;
                var gameRoot = Path.Combine(libraryRoot, "Games", consoleDisplay, Name);
                Directory.CreateDirectory(gameRoot);

                var dlg = new OpenFileDialog
                {
                    Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*",
                };
                if (Directory.Exists(gameRoot)) dlg.InitialDirectory = gameRoot;
                if (dlg.ShowDialog() == true)
                {
                    _pendingCoverPath = dlg.FileName;
                    ConfirmGameCoverCommand.NotifyCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to select cover image:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanConfirmGameCover()
        {
            return !string.IsNullOrWhiteSpace(_pendingCoverPath);
        }

        private void ExecuteConfirmGameCover()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_pendingCoverPath)) return;
                var ext = Path.GetExtension(_pendingCoverPath);
                var settings = _settingsService.Load();
                var libraryRoot = settings.ActiveLibraryPath ?? "C:\\Moodex Library";
                var consoleDisplay = Utilities.ConsoleRegistry.GetDisplayName(ConsoleId) ?? ConsoleId;
                var gameRoot = Path.Combine(libraryRoot, "Games", consoleDisplay, Name);
                Directory.CreateDirectory(gameRoot);
                var dest = Path.Combine(gameRoot, "cover" + ext);
                File.Copy(_pendingCoverPath, dest, overwrite: true);
                _coverConfirmed = true;
                RaisePropertyChanged(nameof(CoverConfirmed));
                UpdateInputsEnabled();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to copy cover image:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateInputsEnabled()
        {
            var enabled = _gameFilesConfirmed && _coverConfirmed;
            SetAllControlsEnabled(enabled);
        }

        // Public read-only flags for UI binding
        public bool GameFilesConfirmed => _gameFilesConfirmed;
        public bool CoverConfirmed => _coverConfirmed;

        // ───────────── command bodies ─────────────
        private void ExecuteBrowse(Window? owner)
        {
            // Prefer starting in the game's data folder created by the helper
            string initialDir = "";
            try
            {
                var settings = _settingsService.Load();
                var libraryRoot = settings.ActiveLibraryPath ?? "C:\\Moodex Library";
                var consoleDisplay = Utilities.ConsoleRegistry.GetDisplayName(ConsoleId) ?? ConsoleId;
                var gameRoot = Path.Combine(libraryRoot, "Games", consoleDisplay, Name);
                var dataDir = Path.Combine(gameRoot, "data");
                if (Directory.Exists(dataDir)) initialDir = dataDir;
            }
            catch { }

            if (IsFolderBasedConsole)
            {
                using var dlg = new System.Windows.Forms.FolderBrowserDialog();
                if (!string.IsNullOrEmpty(initialDir)) dlg.SelectedPath = initialDir;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    FileSystemPath = dlg.SelectedPath;
            }
            else
            {
                var dlg = new OpenFileDialog { Filter = "All Files (*.*)|*.*" };
                if (!string.IsNullOrEmpty(initialDir)) dlg.InitialDirectory = initialDir;
                if (dlg.ShowDialog(owner) == true)
                    FileSystemPath = dlg.FileName;
            }
        }

        private bool CanSave() =>
               !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(FileSystemPath);

        private void ExecuteSave(Window? owner)
        {
            // Create target game root and manifest
            var consoleDisplay = Utilities.ConsoleRegistry.GetDisplayName(ConsoleId) ?? ConsoleId;
            var settings = _settingsService.Load();
            var libraryRoot = settings.ActiveLibraryPath ?? "C:\\Moodex Library";
            var gameRoot = Path.Combine(libraryRoot, "Games", consoleDisplay, Name);
            Directory.CreateDirectory(gameRoot);
            var dataDir = Path.Combine(gameRoot, "data");
            Directory.CreateDirectory(dataDir);

            var guid = System.Guid.NewGuid().ToString();
            var manifest = new GameManifest
            {
                Name = Name,
                Guid = guid,
                AddedDateTime = DateTime.UtcNow,
                ConsoleId = ConsoleId,
                LaunchTarget = string.IsNullOrWhiteSpace(FileSystemPath) ? "" : (IsFolderBasedConsole ? new DirectoryInfo(FileSystemPath).Name : Path.GetFileName(FileSystemPath)),
                LaunchType = IsFolderBasedConsole ? "folder" : "file",
                Genres = SelectedGenres.ToList(),
                ReleaseDateTime = ReleaseDate,
                LastPlayed = null,
                PlayTimeMinutes = 0,
                Archived = false,
                ArchivedDateTime = null,
                Completed = false
            };
            var manifestPath = Path.Combine(gameRoot, ".moodex_game");
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

            // Return a GameInfo for UI
            var launchTarget = manifest.LaunchTarget;
            var filePath = string.IsNullOrWhiteSpace(launchTarget) ? gameRoot : Path.Combine(dataDir, launchTarget);
            NewGame = new GameInfo
            {
                Name = Name,
                ConsoleId = ConsoleId,
                FileSystemPath = filePath,
                Genre = SelectedGenres.Count > 0 ? string.Join(", ", SelectedGenres) : string.Empty,
                ReleaseDate = ReleaseDate,
                IsInArchive = false,
                GameRootPath = gameRoot,
                GameGuid = guid,
                LaunchTarget = launchTarget
            };

            // Cleanup placeholder if present
            try
            {
                var placeholder = Path.Combine(dataDir, "Add game files here");
                if (File.Exists(placeholder)) File.Delete(placeholder);
            }
            catch { }

            if (owner is not null)
            {
                owner.DialogResult = true;
                owner.Close();
            }
        }
    }
}

