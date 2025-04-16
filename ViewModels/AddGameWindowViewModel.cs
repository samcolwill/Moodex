using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using SamsGameLauncher.Models;
using SamsGameLauncher.Commands;
using SamsGameLauncher.Constants;

namespace SamsGameLauncher.ViewModels
{
    public class AddGameWindowViewModel : BaseViewModel
    {
        // backing fields for form inputs
        private string _name = "";
        private string _selectedGameType = "";
        private Emulator? _selectedEmulator;
        private string _selectedConsole = "";
        private string _selectedGenre = "";
        private DateTime _releaseDate = DateTime.Today;
        private string _gamePath = "";

        // dropdown sources
        public List<string> GameTypes { get; }
        public ObservableCollection<Emulator> Emulators { get; }
        public IReadOnlyList<string> Consoles { get; }
        public IReadOnlyList<string> Genres { get; }

        // form-bound properties
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                RaisePropertyChanged();
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged(); // update Save button state
            }
        }

        public string SelectedGameType
        {
            get => _selectedGameType;
            set
            {
                _selectedGameType = value;
                RaisePropertyChanged();
                UpdateEnabledState();           // enable other controls
                ShowEmulator = !value.Equals("Native", StringComparison.OrdinalIgnoreCase);
                UpdateGameFileLabel();          // adjust label text
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }

        public Emulator? SelectedEmulator
        {
            get => _selectedEmulator;
            set
            {
                _selectedEmulator = value!;
                RaisePropertyChanged();
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
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
            set
            {
                _gamePath = value;
                RaisePropertyChanged();
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }

        // UI state: whether each control is enabled
        public bool IsGamePathEnabled { get; private set; }
        public bool IsEmulatorEnabled { get; private set; }
        public bool IsConsoleEnabled { get; private set; }
        public bool IsGenreEnabled { get; private set; }
        public bool IsReleaseDateEnabled { get; private set; }
        public bool IsBrowseEnabled { get; private set; }

        // show/hide emulator controls for Native type
        private bool _showEmulator;
        public bool ShowEmulator
        {
            get => _showEmulator;
            private set { _showEmulator = value; RaisePropertyChanged(); }
        }

        // label text for the file/folder field
        public string GameFileLabelText { get; private set; }

        // commands bound to buttons
        public ICommand BrowseCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // the newly created game object
        public GameBase? NewGame { get; private set; }

        public AddGameWindowViewModel(IEnumerable<Emulator> availableEmulators)
        {
            // initialize sources
            GameTypes = Enum.GetNames(typeof(GameType)).ToList();
            Emulators = new ObservableCollection<Emulator>(availableEmulators);
            Consoles = LauncherConstants.Consoles;
            Genres = LauncherConstants.Genres;
            GameFileLabelText = "Game File:";
            SetAllControlsEnabled(false);

            // wire up commands
            BrowseCommand = new RelayCommand(p => ExecuteBrowse(p as Window));
            SaveCommand = new RelayCommand(p => ExecuteSave(p as Window), p => CanSave());
            CancelCommand = new RelayCommand(p => (p as Window)?.Close());
        }

        // enable/disable all controls at once
        private void SetAllControlsEnabled(bool enabled)
        {
            IsGamePathEnabled = enabled;
            IsEmulatorEnabled = enabled;
            IsConsoleEnabled = enabled;
            IsGenreEnabled = enabled;
            IsReleaseDateEnabled = enabled;
            IsBrowseEnabled = enabled;
        }

        // turn controls on once game type is chosen
        private void UpdateEnabledState() =>
            SetAllControlsEnabled(!string.IsNullOrEmpty(SelectedGameType));

        // adjust the label based on type
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

        // open file or folder dialog
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
                var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "All Files (*.*)|*.*" };
                if (dlg.ShowDialog(owner) == true)
                    GamePath = dlg.FileName;
            }
        }

        // validation for Save button
        private bool CanSave() =>
               !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(GamePath)
            && (!SelectedGameType.Equals("Emulated", StringComparison.OrdinalIgnoreCase)
                || SelectedEmulator != null);

        // create and close dialog
        private void ExecuteSave(Window? owner)
        {
            switch (SelectedGameType)
            {
                case "Emulated":
                    NewGame = new EmulatedGame
                    {
                        Name = Name,
                        GamePath = GamePath,
                        EmulatorId = SelectedEmulator!.Id,
                        Console = SelectedConsole,
                        Genre = SelectedGenre,
                        ReleaseDate = ReleaseDate
                    };
                    break;

                case "Native":
                    NewGame = new NativeGame
                    {
                        Name = Name,
                        ExePath = GamePath,
                        Console = SelectedConsole,
                        Genre = SelectedGenre,
                        ReleaseDate = ReleaseDate
                    };
                    break;

                case "FolderBased":
                    NewGame = new FolderBasedGame
                    {
                        Name = Name,
                        FolderPath = GamePath,
                        EmulatorId = SelectedEmulator!.Id,
                        Console = SelectedConsole,
                        Genre = SelectedGenre,
                        ReleaseDate = ReleaseDate
                    };
                    break;
            }

            // close dialog with success result
            if (owner != null)
            {
                owner.DialogResult = true;
                owner.Close();
            }
        }
    }
}