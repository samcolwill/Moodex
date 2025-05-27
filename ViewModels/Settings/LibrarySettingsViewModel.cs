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

        private ConsoleInfo? _selectedConsole;
        public ConsoleInfo? SelectedConsole
        {
            get => _selectedConsole;
            set
            {
                if (_selectedConsole == value) return;
                _selectedConsole = value;
                OnPropertyChanged();
                EditConsoleCommand.NotifyCanExecuteChanged();
                RemoveConsoleCommand.NotifyCanExecuteChanged();
            }
        }

        // ─── the scrollable, editable list ────────────────
        public ObservableCollection<ConsoleInfo> Consoles { get; }

        // ─── commands for the three buttons ──────────────
        public IRelayCommand AddConsoleCommand { get; }
        public IRelayCommand EditConsoleCommand { get; }
        public IRelayCommand RemoveConsoleCommand { get; }

        public LibrarySettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            var cfg = _settingsService.Load();
            Consoles = new ObservableCollection<ConsoleInfo>(cfg.Consoles);

            AddConsoleCommand = new RelayCommand(OnAddConsole);
            EditConsoleCommand = new RelayCommand(OnEditConsole, () => SelectedConsole is not null);
            RemoveConsoleCommand = new RelayCommand(OnRemoveConsole, () => SelectedConsole is not null);
        }

        private void OnAddConsole()
        {
            // e.g. show a dialog asking for Id & Name
            var dlg = new InputTwoFieldDialog("Add Console",
                                              "Console ID:",
                                              "Console Name:");
            if (dlg.ShowDialog() != true) return;

            var newInfo = new ConsoleInfo
            {
                Id = dlg.Field1Text.Trim(),
                Name = dlg.Field2Text.Trim()
            };
            Consoles.Add(newInfo);
            SaveConsoles();
        }

        private void OnEditConsole()
        {
            if (SelectedConsole == null) return;
            var ci = SelectedConsole;
            var dlg = new InputTwoFieldDialog("Edit Console",
               "Console ID:", "Console Name:",
               ci.Id, ci.Name);
            if (dlg.ShowDialog() != true) return;

            ci.Id = dlg.Field1Text.Trim();
            ci.Name = dlg.Field2Text.Trim();
            var idx = Consoles.IndexOf(ci);
            Consoles.RemoveAt(idx);
            Consoles.Insert(idx, ci);
            SaveConsoles();
        }

        private void OnRemoveConsole()
        {
            if (SelectedConsole == null) return;
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
