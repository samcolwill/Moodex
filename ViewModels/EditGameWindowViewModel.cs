using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;
using CommunityToolkit.Mvvm.Input;                 // ✅ Toolkit commands
using SamsGameLauncher.Constants;
using SamsGameLauncher.Models;

namespace SamsGameLauncher.ViewModels
{
    public class EditGameWindowViewModel : BaseViewModel
    {
        private readonly GameBase _originalGame;

        // ───────────── backing fields ─────────────
        private string _name = "";
        private string _gameFilePath = "";
        private Emulator? _selectedEmulator;
        private string _selectedConsole = "";
        private string _selectedGenre = "";
        private DateTime _releaseDate;
        private bool _showEmulator;

        // ───────────── dropdown sources ─────────────
        public ObservableCollection<Emulator> Emulators { get; }
        public IReadOnlyList<string> Consoles { get; }
        public IReadOnlyList<string> Genres { get; }

        public string GameTypeName { get; }   // display-only

        // ───────────── bindable props ─────────────
        public string Name
        {
            get => _name;
            set { _name = value; RaisePropertyChanged(); SaveCommand.NotifyCanExecuteChanged(); }
        }

        public string GameFilePath
        {
            get => _gameFilePath;
            set { _gameFilePath = value; RaisePropertyChanged(); SaveCommand.NotifyCanExecuteChanged(); }
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

        public bool ShowEmulator
        {
            get => _showEmulator;
            private set { _showEmulator = value; RaisePropertyChanged(); }
        }

        // ───────────── commands (Toolkit) ─────────────
        public IRelayCommand BrowseCommand { get; }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }

        // ───────────── ctor ─────────────
        [SupportedOSPlatform("windows")]
        public EditGameWindowViewModel(
            GameBase gameToEdit,
            IEnumerable<Emulator> availableEmulators)
        {
            _originalGame = gameToEdit;

            Emulators = new ObservableCollection<Emulator>(availableEmulators);
            Consoles = LauncherConstants.Consoles;
            Genres = LauncherConstants.Genres;

            // Toolkit commands
            BrowseCommand = new RelayCommand<Window?>(ExecuteBrowse);
            SaveCommand = new RelayCommand<Window?>(ExecuteSave, _ => CanSave());
            CancelCommand = new RelayCommand<Window?>(w => w?.Close());

            // populate fields
            Name = gameToEdit.Name;
            SelectedConsole = gameToEdit.Console;
            SelectedGenre = gameToEdit.Genre;
            ReleaseDate = gameToEdit.ReleaseDate;
            GameTypeName = gameToEdit.GameType.ToString();

            switch (gameToEdit)
            {
                case EmulatedGame em:
                    GameFilePath = em.GamePath;
                    SelectedEmulator = Emulators.FirstOrDefault(e => e.Id == em.EmulatorId);
                    ShowEmulator = true;
                    break;

                case NativeGame ng:
                    GameFilePath = ng.ExePath;
                    ShowEmulator = false;
                    break;

                case FolderBasedGame fg:
                    GameFilePath = fg.FolderPath;
                    ShowEmulator = false;
                    break;
            }
        }

        // ───────────── command bodies ─────────────
        [SupportedOSPlatform("windows")]
        private void ExecuteBrowse(Window? owner)
        {
            if (_originalGame is FolderBasedGame)
            {
                var fb = new System.Windows.Forms.FolderBrowserDialog();
                if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    GameFilePath = fb.SelectedPath;
            }
            else
            {
                var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "All Files (*.*)|*.*" };
                if (ofd.ShowDialog(owner) == true)
                    GameFilePath = ofd.FileName;
            }
        }

        private bool CanSave()
        {
            var basic = !string.IsNullOrWhiteSpace(Name) &&
                        !string.IsNullOrWhiteSpace(GameFilePath);

            return GameTypeName.Equals("Emulated", StringComparison.OrdinalIgnoreCase)
                   ? basic && SelectedEmulator != null
                   : basic;
        }

        private void ExecuteSave(Window? owner)
        {
            // update shared fields
            _originalGame.Name = Name;
            _originalGame.Console = SelectedConsole;
            _originalGame.Genre = SelectedGenre;
            _originalGame.ReleaseDate = ReleaseDate;

            switch (_originalGame)
            {
                case EmulatedGame em:
                    em.GamePath = GameFilePath;
                    em.EmulatorId = SelectedEmulator?.Id ?? "";
                    em.Emulator = SelectedEmulator;
                    break;

                case NativeGame ng:
                    ng.ExePath = GameFilePath;
                    break;

                case FolderBasedGame fg:
                    fg.FolderPath = GameFilePath;
                    break;
            }

            if (owner is not null)
            {
                owner.DialogResult = true;
                owner.Close();
            }
        }
    }
}
