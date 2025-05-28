using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using SamsGameLauncher.Services;
using SamsGameLauncher.Models;
using CommunityToolkit.Mvvm.Input;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SamsGameLauncher.ViewModels
{
    public class AddGameWindowViewModel : BaseViewModel
    {
        // ───────────── backing fields ─────────────
        private string _name = "";
        private string _consoleId = "";
        private string _fileSystemPath = "";
        private string _genre = "";
        private DateTime _releaseDate = DateTime.Today;

        private static readonly HashSet<string> _folderConsoleIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "Playstation3"
        };
        private bool IsFolderBasedConsole => !string.IsNullOrEmpty(ConsoleId)
            && _folderConsoleIds.Contains(ConsoleId);

        // ───────────── dropdown sources ─────────────
        public IReadOnlyList<ConsoleInfo> Consoles { get; }
        public IReadOnlyList<string> Genres { get; }

        // ───────────── form-bound properties ─────────────
        public string Name
        {
            get => _name;
            set { _name = value; RaisePropertyChanged(); SaveCommand.NotifyCanExecuteChanged(); }
        }

        public string ConsoleId
        {
            get => _consoleId;
            set
            {
                if (_consoleId == value) return;
                _consoleId = value;
                RaisePropertyChanged();

                SetAllControlsEnabled(true);
                UpdateFileSystemPathLabel();
                SaveCommand.NotifyCanExecuteChanged();
            }
        }

        public string Genre
        {
            get => _genre;
            set { _genre = value; RaisePropertyChanged(); }
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
        public IRelayCommand CancelCommand { get; }

        // result object
        public GameInfo? NewGame { get; private set; }

        // ───────────── ctor ─────────────
        public AddGameWindowViewModel(
            ISettingsService settingsService)
        {
            var settings = settingsService.Load();
            Consoles = settings.Consoles;
            Genres = settings.Genres;

            GameFileLabelText = "Game File:";
            SetAllControlsEnabled(false);

            // Toolkit commands (strongly typed generic)
            BrowseCommand = new RelayCommand<Window?>(ExecuteBrowse);
            SaveCommand = new RelayCommand<Window?>(ExecuteSave, _ => CanSave());
            CancelCommand = new RelayCommand<Window?>(w => w?.Close());
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

        // ───────────── command bodies ─────────────
        private void ExecuteBrowse(Window? owner)
        {
            if (IsFolderBasedConsole)
            {
                using var dlg = new System.Windows.Forms.FolderBrowserDialog();
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    FileSystemPath = dlg.SelectedPath;
            }
            else
            {
                var dlg = new OpenFileDialog { Filter = "All Files (*.*)|*.*" };
                if (dlg.ShowDialog(owner) == true)
                    FileSystemPath = dlg.FileName;
            }
        }

        private bool CanSave() =>
               !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(FileSystemPath);

        private void ExecuteSave(Window? owner)
        {
            NewGame = new GameInfo
            {
                Name = Name,
                ConsoleId = ConsoleId,
                FileSystemPath = FileSystemPath,
                Genre = Genre,
                ReleaseDate = ReleaseDate
            };

            if (owner is not null)
            {
                owner.DialogResult = true;
                owner.Close();
            }
        }
    }
}
