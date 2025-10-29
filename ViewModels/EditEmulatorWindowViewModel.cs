using CommunityToolkit.Mvvm.Input;
using Moodex.Models;
using Moodex.Services;
using System;
using System.Windows;

namespace Moodex.ViewModels
{
    public class EditEmulatorWindowViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly EmulatorInfo _originalEmulator;

        // ───────────── backing fields ─────────────
        private string _id;
        private string _name = "";
        private string _executablePath = "";
        private string _defaultArguments = "";
        private string _emulatedConsoleId = "";

        // ───────────── dropdown source ─────────────
        public IReadOnlyList<ConsoleInfo> Consoles { get; }

        // ───────────── bindable props ─────────────
        public string Id
        {
            get => _id;
            // read-only in the UI
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                RaisePropertyChanged();
                SaveCommand.NotifyCanExecuteChanged();
            }
        }

        public string ExecutablePath
        {
            get => _executablePath;
            set
            {
                if (_executablePath == value) return;
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
                if (_defaultArguments == value) return;
                _defaultArguments = value;
                RaisePropertyChanged();
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

        // ───────────── commands ─────────────
        public IRelayCommand<Window?> BrowseCommand { get; }
        public IRelayCommand<Window?> SaveCommand { get; }
        public IRelayCommand<Window?> CancelCommand { get; }

        // ───────────── ctor ─────────────
        public EditEmulatorWindowViewModel(ISettingsService settingsService, EmulatorInfo emulator)
        {
            _settingsService = settingsService
                                   ?? throw new ArgumentNullException(nameof(settingsService));

            _originalEmulator = emulator
                ?? throw new ArgumentNullException(nameof(emulator));

            // Load the console definitions once:
            var cfg = _settingsService.Load();
            Consoles = cfg.Consoles;

            // initialize backing fields from the original model
            _id = emulator.Id;
            _name = emulator.Name;
            _executablePath = emulator.ExecutablePath;
            _defaultArguments = emulator.DefaultArguments;
            _emulatedConsoleId = emulator.EmulatedConsoleId;

            // wire up commands
            BrowseCommand = new RelayCommand<Window?>(ExecuteBrowse);
            SaveCommand = new RelayCommand<Window?>(ExecuteSave, _ => CanSave());
            CancelCommand = new RelayCommand<Window?>(w => w?.Close());
        }

        // ───────────── command bodies ─────────────
        private void ExecuteBrowse(Window? owner)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog(owner) == true)
                ExecutablePath = dlg.FileName;
        }

        private bool CanSave()
        {
            return
                !string.IsNullOrWhiteSpace(Name) &&
                !string.IsNullOrWhiteSpace(ExecutablePath);
        }

        private void ExecuteSave(Window? owner)
        {
            // copy edits back into the original model
            _originalEmulator.Name = Name;
            _originalEmulator.ExecutablePath = ExecutablePath;
            _originalEmulator.DefaultArguments = DefaultArguments;
            _originalEmulator.EmulatedConsoleId = EmulatedConsoleId;

            if (owner != null)
            {
                owner.DialogResult = true;
                owner.Close();
            }
        }
    }
}

