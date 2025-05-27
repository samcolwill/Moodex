using CommunityToolkit.Mvvm.Input;
using SamsGameLauncher.Models;
using SamsGameLauncher.Services;
using System.Windows;
using System.Windows.Input;

namespace SamsGameLauncher.ViewModels
{
    // ViewModel for AddEmulatorWindow.xaml
    public class AddEmulatorWindowViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;

        // backing fields for input fields
        private string _id = "";
        private string _name = "";
        private string _emulatedConsoleId = "";
        private string _executablePath = "";
        private string _defaultArguments = "";

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                RaisePropertyChanged();
                SaveCommand.NotifyCanExecuteChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                RaisePropertyChanged();
                SaveCommand.NotifyCanExecuteChanged();
            }
        }

        public string EmulatedConsoleId
        {
            get => _emulatedConsoleId;
            set
            {
                if (_emulatedConsoleId == value) return;
                _emulatedConsoleId = value;
                RaisePropertyChanged();
                SaveCommand.NotifyCanExecuteChanged();
            }
        }

        public string ExecutablePath
        {
            get => _executablePath;
            set
            {
                _executablePath = value;
                RaisePropertyChanged();
                SaveCommand.NotifyCanExecuteChanged();
            }
        }

        public string DefaultArguments
        {
            get => _defaultArguments;
            set
            {
                _defaultArguments = value;
                RaisePropertyChanged();
            }
        }

        public IReadOnlyList<ConsoleInfo> Consoles { get; }

        public EmulatorInfo? NewEmulator { get; private set; }

        // commands bound to buttons in the view
        public IRelayCommand BrowseCommand { get; }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }

        public AddEmulatorWindowViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            Consoles = _settingsService.Load().Consoles.ToArray();

            BrowseCommand = new RelayCommand<Window?>(ExecuteBrowse);
            SaveCommand = new RelayCommand<Window?>(ExecuteSave, _ => CanSave());
            CancelCommand = new RelayCommand<Window?>(w => w?.Close());
        }

        // opens OpenFileDialog for selecting the EXE
        private void ExecuteBrowse(Window? owner)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
                ExecutablePath = dlg.FileName;
        }

        // determines whether SaveCommand can run
        private bool CanSave()
        {
            return
                !string.IsNullOrWhiteSpace(Id) &&
                !string.IsNullOrWhiteSpace(Name) &&
                !string.IsNullOrWhiteSpace(ExecutablePath);
        }

        // called when user clicks "Add Emulator"
        private void ExecuteSave(Window? owner)
        {
            // construct the new Emulator instance
            NewEmulator = new EmulatorInfo
            {
                Id = Id,
                Name = Name,
                EmulatedConsoleId = EmulatedConsoleId,
                ExecutablePath = ExecutablePath,
                DefaultArguments = DefaultArguments
            };

            // close dialog with success result
            if (owner != null)
            {
                owner.DialogResult = true;
                owner.Close();
            }
        }
    }
}
