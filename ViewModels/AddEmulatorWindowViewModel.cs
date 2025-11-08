using CommunityToolkit.Mvvm.Input;
using Moodex.Models;
using Moodex.Services;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace Moodex.ViewModels
{
    // ViewModel for AddEmulatorWindow.xaml
    public class AddEmulatorWindowViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;

        // backing fields for input fields
        private string _id = "";
        private string _name = "";
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

        // Multi-console selection
        public class SelectableConsole : BaseViewModel
        {
            public ConsoleInfo Console { get; set; } = new ConsoleInfo();
            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set { if (_isSelected != value) { _isSelected = value; RaisePropertyChanged(); } }
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

        public ObservableCollection<SelectableConsole> ConsoleChoices { get; } = new();

        public EmulatorInfo? NewEmulator { get; private set; }

        // commands bound to buttons in the view
        public IRelayCommand BrowseCommand { get; }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }

        public AddEmulatorWindowViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            foreach (var c in _settingsService.Load().Consoles)
            {
                ConsoleChoices.Add(new SelectableConsole { Console = c, IsSelected = false });
            }

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
                Guid = System.Guid.NewGuid().ToString(),
                    EmulatedConsoleIds = ConsoleChoices.Where(x => x.IsSelected).Select(x => x.Console.Id).ToList(),
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

