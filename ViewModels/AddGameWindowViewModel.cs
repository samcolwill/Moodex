using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using SamsGameLauncher.Constants;
using SamsGameLauncher.Models;
using CommunityToolkit.Mvvm.Input;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SamsGameLauncher.ViewModels
{
    public class AddGameWindowViewModel : BaseViewModel
    {
        // ───────────── backing fields ─────────────
        private string _name = "";
        private string _selectedGameType = "";
        private Emulator? _selectedEmulator;
        private string _selectedConsole = "";
        private string _selectedGenre = "";
        private DateTime _releaseDate = DateTime.Today;
        private string _gamePath = "";

        // ───────────── dropdown sources ─────────────
        public List<string> GameTypes { get; }
        public ObservableCollection<Emulator> Emulators { get; }
        public IReadOnlyList<string> Consoles { get; }
        public IReadOnlyList<string> Genres { get; }

        // ───────────── form-bound properties ─────────────
        public string Name
        {
            get => _name;
            set { _name = value; RaisePropertyChanged(); SaveCommand.NotifyCanExecuteChanged(); }
        }

        public string SelectedGameType
        {
            get => _selectedGameType;
            set
            {
                _selectedGameType = value;
                RaisePropertyChanged();
                UpdateEnabledState();
                ShowEmulator = !value.Equals("Native", StringComparison.OrdinalIgnoreCase);
                UpdateGameFileLabel();
                SaveCommand.NotifyCanExecuteChanged();
            }
        }

        public Emulator? SelectedEmulator
        {
            get => _selectedEmulator;
            set { _selectedEmulator = value; RaisePropertyChanged(); SaveCommand.NotifyCanExecuteChanged(); }
        }

        public string SelectedConsole
        {
            get => _selectedConsole;
            set { _selectedConsole = value; RaisePropertyChanged(); }
        }

        public string SelectedGenre
        {
            get => _selectedGenre;
            set { _selectedGenre = value; RaisePropertyChanged(); }
        }

        public DateTime ReleaseDate
        {
            get => _releaseDate;
            set { _releaseDate = value; RaisePropertyChanged(); }
        }

        public string GamePath
        {
            get => _gamePath;
            set { _gamePath = value; RaisePropertyChanged(); SaveCommand.NotifyCanExecuteChanged(); }
        }

        // ───────────── UI-state flags ─────────────
        private bool _isGamePathEnabled;
        public bool IsGamePathEnabled
        {
            get => _isGamePathEnabled;
            private set { _isGamePathEnabled = value; RaisePropertyChanged(); }
        }

        private bool _isEmulatorEnabled;
        public bool IsEmulatorEnabled
        {
            get => _isEmulatorEnabled;
            private set { _isEmulatorEnabled = value; RaisePropertyChanged(); }
        }

        private bool _isConsoleEnabled;
        public bool IsConsoleEnabled
        {
            get => _isConsoleEnabled;
            private set { _isConsoleEnabled = value; RaisePropertyChanged(); }
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

        // show / hide emulator picker for “Native”
        private bool _showEmulator;
        public bool ShowEmulator
        {
            get => _showEmulator;
            private set { _showEmulator = value; RaisePropertyChanged(); }
        }

        public string GameFileLabelText { get; private set; }

        // ───────────── commands (Toolkit) ─────────────
        public IRelayCommand BrowseCommand { get; }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }

        // result object
        public GameBase? NewGame { get; private set; }

        // ───────────── ctor ─────────────
        public AddGameWindowViewModel(IEnumerable<Emulator> availableEmulators)
        {
            GameTypes = Enum.GetNames(typeof(GameType)).ToList();
            Emulators = new ObservableCollection<Emulator>(availableEmulators);
            Consoles = LauncherConstants.Consoles;
            Genres = LauncherConstants.Genres;

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
            IsGamePathEnabled = enabled;
            IsEmulatorEnabled = enabled;
            IsConsoleEnabled = enabled;
            IsGenreEnabled = enabled;
            IsReleaseDateEnabled = enabled;
            IsBrowseEnabled = enabled;
        }

        private void UpdateEnabledState() =>
            SetAllControlsEnabled(!string.IsNullOrEmpty(SelectedGameType));

        private void UpdateGameFileLabel()
        {
            GameFileLabelText = SelectedGameType switch
            {
                "Emulated" => "Game File:",
                "Native" => "Game Executable:",
                "FolderBased" => "Game Folder:",
                _ => "Game File:"
            };
            RaisePropertyChanged(nameof(GameFileLabelText));
        }

        // ───────────── command bodies ─────────────
        private void ExecuteBrowse(Window? owner)
        {
            if (SelectedGameType.Equals("FolderBased", StringComparison.OrdinalIgnoreCase))
            {
                var dlg = new System.Windows.Forms.FolderBrowserDialog();
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    GamePath = dlg.SelectedPath;
            }
            else
            {
                var dlg = new OpenFileDialog { Filter = "All Files (*.*)|*.*" };
                if (dlg.ShowDialog(owner) == true)
                    GamePath = dlg.FileName;
            }
        }

        private bool CanSave() =>
               !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(GamePath)
            && (!SelectedGameType.Equals("Emulated", StringComparison.OrdinalIgnoreCase)
                || SelectedEmulator != null);

        private void ExecuteSave(Window? owner)
        {
            NewGame = SelectedGameType switch
            {
                "Emulated" => new EmulatedGame
                {
                    Name = Name,
                    GamePath = GamePath,
                    EmulatorId = SelectedEmulator!.Id,
                    Console = SelectedConsole,
                    Genre = SelectedGenre,
                    ReleaseDate = ReleaseDate
                },
                "Native" => new NativeGame
                {
                    Name = Name,
                    ExePath = GamePath,
                    Console = SelectedConsole,
                    Genre = SelectedGenre,
                    ReleaseDate = ReleaseDate
                },
                "FolderBased" => new FolderBasedGame
                {
                    Name = Name,
                    FolderPath = GamePath,
                    EmulatorId = SelectedEmulator!.Id,
                    Console = SelectedConsole,
                    Genre = SelectedGenre,
                    ReleaseDate = ReleaseDate
                },
                _ => null
            };

            if (owner is not null)
            {
                owner.DialogResult = true;
                owner.Close();
            }
        }
    }
}
