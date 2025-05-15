using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Input;
using SamsGameLauncher.Services;
using SamsGameLauncher.Views;

namespace SamsGameLauncher.ViewModels.Settings
{
    public class LibrarySettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;

        // ─── backing field for selection ─────────────────
        private string? _selectedConsole;
        public string? SelectedConsole
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
        public ObservableCollection<string> Consoles { get; }

        // ─── commands for the three buttons ──────────────
        public IRelayCommand AddConsoleCommand { get; }
        public IRelayCommand EditConsoleCommand { get; }
        public IRelayCommand RemoveConsoleCommand { get; }

        public LibrarySettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            // seed from JSON
            var cfg = _settingsService.Load();
            Consoles = new ObservableCollection<string>(cfg.Consoles);

            // wire up commands
            AddConsoleCommand = new RelayCommand(OnAddConsole);
            EditConsoleCommand = new RelayCommand(OnEditConsole, () => !string.IsNullOrEmpty(SelectedConsole));
            RemoveConsoleCommand = new RelayCommand(OnRemoveConsole, () => !string.IsNullOrEmpty(SelectedConsole));
        }

        private void OnAddConsole()
        {
            var dlg = new InputDialog("Add Console", "Enter new console name:");
            if (dlg.ShowDialog() != true) return;

            var name = dlg.InputText?.Trim();
            if (string.IsNullOrEmpty(name) || Consoles.Contains(name))
                return;

            Consoles.Add(name);
            SaveConsoles();
        }

        private void OnEditConsole()
        {
            if (SelectedConsole is null) return;
            var dlg = new InputDialog("Edit Console",
                                      "Modify console name:",
                                      SelectedConsole);
            if (dlg.ShowDialog() != true) return;

            var name = dlg.InputText?.Trim();
            if (string.IsNullOrEmpty(name) || Consoles.Contains(name))
                return;

            var idx = Consoles.IndexOf(SelectedConsole);
            Consoles[idx] = name;
            SelectedConsole = name;
            SaveConsoles();
        }

        private void OnRemoveConsole()
        {
            if (SelectedConsole is null) return;
            Consoles.Remove(SelectedConsole);
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
