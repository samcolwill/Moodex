using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using SamsGameLauncher.Models;
using SamsGameLauncher.Commands;
using SamsGameLauncher.Constants;

namespace SamsGameLauncher.ViewModels
{
    public class EditGameWindowViewModel : BaseViewModel
    {
        private readonly GameBase _originalGame;       // reference to update on save

        // backing fields
        private string _name = string.Empty;
        private string _gameFilePath = string.Empty;
        private Emulator? _selectedEmulator;
        private string _selectedConsole = string.Empty;
        private string _selectedGenre = string.Empty;
        private DateTime _releaseDate;
        private bool _showEmulator;

        // dropdown sources
        public ObservableCollection<Emulator> Emulators { get; }
        public IReadOnlyList<string> Consoles { get; }
        public IReadOnlyList<string> Genres { get; }

        public string GameTypeName { get; }      // display only, e.g. "Emulated"

        // bindable props
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                RaisePropertyChanged();
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }

        public string GameFilePath
        {
            get => _gameFilePath;
            set
            {
                _gameFilePath = value;
                RaisePropertyChanged();
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }

        public Emulator? SelectedEmulator
        {
            get => _selectedEmulator;
            set
            {
                _selectedEmulator = value;
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

        public bool ShowEmulator
        {
            get => _showEmulator;
            private set { _showEmulator = value; RaisePropertyChanged(); }
        }

        // UI commands
        public ICommand BrowseCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditGameWindowViewModel(GameBase gameToEdit, IEnumerable<Emulator> availableEmulators)
        {
            _originalGame = gameToEdit;

            Emulators = new ObservableCollection<Emulator>(availableEmulators);
            Consoles = LauncherConstants.Consoles;
            Genres = LauncherConstants.Genres;

            // initialize commands before setting properties
            BrowseCommand = new RelayCommand(p => ExecuteBrowse(p as Window));
            SaveCommand = new RelayCommand(p => ExecuteSave(p as Window), p => CanSave());
            CancelCommand = new RelayCommand(p => (p as Window)?.Close());

            // populate common fields
            Name = gameToEdit.Name;
            SelectedConsole = gameToEdit.Console;
            SelectedGenre = gameToEdit.Genre;
            ReleaseDate = gameToEdit.ReleaseDate;
            GameTypeName = gameToEdit.GameType.ToString();

            // subtype‐specific setup
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

        private void ExecuteBrowse(Window? owner)
        {
            if (_originalGame is FolderBasedGame)
            {
                var dlg = new System.Windows.Forms.FolderBrowserDialog();
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    GameFilePath = dlg.SelectedPath;
            }
            else
            {
                var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "All Files (*.*)|*.*" };
                if (dlg.ShowDialog(owner) == true)
                    GameFilePath = dlg.FileName;
            }
        }

        private bool CanSave()
        {
            bool basic =
                !string.IsNullOrWhiteSpace(Name) &&
                !string.IsNullOrWhiteSpace(GameFilePath);

            // require emulator for Emulated type
            if (GameTypeName.Equals("Emulated", StringComparison.OrdinalIgnoreCase))
                return basic && SelectedEmulator != null;

            return basic;
        }

        private void ExecuteSave(Window? owner)
        {
            // update the original model instance
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

            // close dialog with success result
            if (owner != null)
            {
                owner.DialogResult = true;
                owner.Close();
            }
        }
    }
}
