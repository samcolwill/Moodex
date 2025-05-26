using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Input;
using SamsGameLauncher.Services;
using SamsGameLauncher.Models;
using SamsGameLauncher.Views.Utilities;

namespace SamsGameLauncher.ViewModels.Settings
{
    public class LibrarySettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;

        // ─── backing field for selection ─────────────────
        private ConsoleType? _selectedConsole;
        public ConsoleType? SelectedConsole
        {
            get => _selectedConsole;
            set
            {
                if (_selectedConsole == value) return;
                _selectedConsole = value;
                OnPropertyChanged();
                // Update Edit/Remove availability
                EditConsoleCommand.NotifyCanExecuteChanged();
                RemoveConsoleCommand.NotifyCanExecuteChanged();
            }
        }

        // ─── the scrollable, editable list ────────────────
        public ObservableCollection<ConsoleType> Consoles { get; }

        // ─── commands for the three buttons ──────────────
        public IRelayCommand AddConsoleCommand { get; }
        public IRelayCommand EditConsoleCommand { get; }
        public IRelayCommand RemoveConsoleCommand { get; }

        public LibrarySettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            // seed from JSON
            var cfg = _settingsService.Load();
            Consoles = new ObservableCollection<ConsoleType>(cfg.Consoles);

            // wire up commands
            AddConsoleCommand = new RelayCommand(OnAddConsole);
            EditConsoleCommand = new RelayCommand(OnEditConsole, () => SelectedConsole.HasValue);
            RemoveConsoleCommand = new RelayCommand(OnRemoveConsole, () => SelectedConsole.HasValue);
        }

        private void OnAddConsole()
        {
            var dlg = new InputDialog("Add Console", "Enter new console name:");
            if (dlg.ShowDialog() != true) return;

            var input = dlg.InputText?.Trim();
            if (string.IsNullOrEmpty(input)) return;

            // Try parse into enum
            if (!Enum.TryParse<ConsoleType>(input, ignoreCase: true, out var consoleType))
                return;  // or show validation error

            if (Consoles.Contains(consoleType)) return;

            Consoles.Add(consoleType);
            SaveConsoles();
        }

        private void OnEditConsole()
        {
            if (SelectedConsole is not ConsoleType oldConsole) return;

            var dlg = new InputDialog(
                "Edit Console",
                "Modify console name:",
                oldConsole.ToString());
            if (dlg.ShowDialog() != true) return;

            var input = dlg.InputText?.Trim();
            if (string.IsNullOrEmpty(input)) return;
            if (!Enum.TryParse<ConsoleType>(input, ignoreCase: true, out var newConsole))
                return;  // or show validation error
            if (Consoles.Contains(newConsole)) return;

            var idx = Consoles.IndexOf(oldConsole);
            Consoles[idx] = newConsole;
            SelectedConsole = newConsole;
            SaveConsoles();
        }

        private void OnRemoveConsole()
        {
            if (SelectedConsole is not ConsoleType consoleToRemove) return;

            Consoles.Remove(consoleToRemove);
            SelectedConsole = null;
            SaveConsoles();
        }

        private void SaveConsoles()
        {
            var cfg = _settingsService.Load();
            cfg.Consoles = Consoles.ToList();
            _settingsService.Save(cfg);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
