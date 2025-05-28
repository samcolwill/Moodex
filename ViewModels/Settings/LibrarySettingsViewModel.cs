using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Input;
using SamsGameLauncher.Models;
using SamsGameLauncher.Services;
using SamsGameLauncher.Views.Utilities;

namespace SamsGameLauncher.ViewModels.Settings
{
    public class LibrarySettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;

        // ─── Consoles (unchanged) ──────────────────────────────────────────
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
        public ObservableCollection<ConsoleInfo> Consoles { get; }
        public IRelayCommand AddConsoleCommand { get; }
        public IRelayCommand EditConsoleCommand { get; }
        public IRelayCommand RemoveConsoleCommand { get; }

        // ─── Genres (new) ─────────────────────────────────────────────────
        private string? _selectedGenre;
        public string? SelectedGenre
        {
            get => _selectedGenre;
            set
            {
                if (_selectedGenre == value) return;
                _selectedGenre = value;
                OnPropertyChanged();
                EditGenreCommand.NotifyCanExecuteChanged();
                RemoveGenreCommand.NotifyCanExecuteChanged();
            }
        }
        public ObservableCollection<string> Genres { get; }
        public IRelayCommand AddGenreCommand { get; }
        public IRelayCommand EditGenreCommand { get; }
        public IRelayCommand RemoveGenreCommand { get; }

        public LibrarySettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            var cfg = _settingsService.Load();

            // Consoles setup
            Consoles = new ObservableCollection<ConsoleInfo>(cfg.Consoles);
            AddConsoleCommand = new RelayCommand(OnAddConsole);
            EditConsoleCommand = new RelayCommand(OnEditConsole, () => SelectedConsole != null);
            RemoveConsoleCommand = new RelayCommand(OnRemoveConsole, () => SelectedConsole != null);

            // Genres setup (mirror consoles)
            Genres = new ObservableCollection<string>(cfg.Genres);
            AddGenreCommand = new RelayCommand(OnAddGenre);
            EditGenreCommand = new RelayCommand(OnEditGenre, () => SelectedGenre != null);
            RemoveGenreCommand = new RelayCommand(OnRemoveGenre, () => SelectedGenre != null);
        }

        // ─── Consoles handlers (unchanged) ───────────────────────────────
        private void OnAddConsole()
        {
            var dlg = new InputTwoFieldDialog(
                "Add Console",
                "Console ID:", "Console Name:");
            if (dlg.ShowDialog() != true) return;

            var newInfo = new ConsoleInfo
            {
                Id = dlg.Field1Text.Trim(),
                Name = dlg.Field2Text.Trim()
            };
            Consoles.Add(newInfo);
            SaveSettings();
        }

        private void OnEditConsole()
        {
            if (SelectedConsole == null) return;
            var ci = SelectedConsole;
            var dlg = new InputTwoFieldDialog(
                "Edit Console",
                "Console ID:", "Console Name:",
                ci.Id, ci.Name);
            if (dlg.ShowDialog() != true) return;

            ci.Id = dlg.Field1Text.Trim();
            ci.Name = dlg.Field2Text.Trim();
            var idx = Consoles.IndexOf(ci);
            Consoles[idx] = ci;
            SaveSettings();
        }

        private void OnRemoveConsole()
        {
            if (SelectedConsole == null) return;
            Consoles.Remove(SelectedConsole);
            SelectedConsole = null;
            SaveSettings();
        }

        // ─── Genres handlers (new) ───────────────────────────────────────
        private void OnAddGenre()
        {
            var dlg = new InputOneFieldDialog(
                "Add Genre",
                "Genre Name:",
                defaultText: "");
            if (dlg.ShowDialog() != true) return;

            var genre = dlg.InputText.Trim();
            if (string.IsNullOrEmpty(genre) || Genres.Contains(genre))
                return;

            Genres.Add(genre);
            SaveSettings();
        }

        private void OnEditGenre()
        {
            if (SelectedGenre == null) return;
            var dlg = new InputOneFieldDialog(
                "Edit Genre",
                "Genre Name:",
                defaultText: SelectedGenre);
            if (dlg.ShowDialog() != true) return;

            var newValue = dlg.InputText.Trim();
            if (string.IsNullOrEmpty(newValue)) return;

            var idx = Genres.IndexOf(SelectedGenre);
            Genres[idx] = newValue;
            SelectedGenre = newValue;
            SaveSettings();
        }

        private void OnRemoveGenre()
        {
            if (SelectedGenre == null) return;
            Genres.Remove(SelectedGenre);
            SelectedGenre = null;
            SaveSettings();
        }

        // ─── Persist both lists in one shot ──────────────────────────────
        private void SaveSettings()
        {
            var cfg = _settingsService.Load();
            cfg.Consoles = Consoles.ToList();
            cfg.Genres = Genres.ToList();
            _settingsService.Save(cfg);
        }

        // ─── INotifyPropertyChanged ─────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
