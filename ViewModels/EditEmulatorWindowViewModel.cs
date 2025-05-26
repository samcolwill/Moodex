using System;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using SamsGameLauncher.Models;

namespace SamsGameLauncher.ViewModels
{
    public class EditEmulatorWindowViewModel : BaseViewModel
    {
        private readonly Emulator _originalEmulator;

        // ───────────── backing fields ─────────────
        private string _id;
        private string _name = "";
        private string _executablePath = "";
        private string _defaultArguments = "";
        private string _targetConsole = "";

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

        public string TargetConsole
        {
            get => _targetConsole;
            set
            {
                _targetConsole = value;
                RaisePropertyChanged();
                SaveCommand.NotifyCanExecuteChanged();
            }
        }

        // ───────────── commands ─────────────
        public IRelayCommand<Window?> BrowseCommand { get; }
        public IRelayCommand<Window?> SaveCommand { get; }
        public IRelayCommand<Window?> CancelCommand { get; }

        // ───────────── ctor ─────────────
        public EditEmulatorWindowViewModel(Emulator emulator)
        {
            _originalEmulator = emulator
                ?? throw new ArgumentNullException(nameof(emulator));

            // initialize backing fields from the original model
            _id = emulator.Id;
            _name = emulator.Name;
            _executablePath = emulator.ExecutablePath;
            _defaultArguments = emulator.DefaultArguments;
            _targetConsole = emulator.TargetConsole;

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
            _originalEmulator.TargetConsole = TargetConsole;

            if (owner != null)
            {
                owner.DialogResult = true;
                owner.Close();
            }
        }
    }
}
