using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using Moodex.Configuration;
using Moodex.Converters;
using Moodex.Models;
using Moodex.Models.Manifests;
using Moodex.Services;
using Moodex.Services.Igdb;
using Moodex.Utilities;
using Moodex.Views.Utilities;

namespace Moodex.ViewModels.Settings
{
    public class LibrarySettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;
        private readonly IIgdbService _igdbService;
        private readonly MoodexState _moodexState;
        private readonly SettingsModel _model;

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

        // ─── IGDB ────────────────────────────────────────────────────────────
        private bool _igdbEnabled;
        public bool IgdbEnabled
        {
            get => _igdbEnabled;
            set
            {
                if (_igdbEnabled != value)
                {
                    _igdbEnabled = value;
                    _model.Igdb.Enabled = value;
                    _settingsService.Save(_model);
                    OnPropertyChanged();
                }
            }
        }

        private string _igdbStatusMessage = string.Empty;
        public string IgdbStatusMessage
        {
            get => _igdbStatusMessage;
            private set { if (_igdbStatusMessage != value) { _igdbStatusMessage = value; OnPropertyChanged(); } }
        }

        public IAsyncRelayCommand ScanForMissingDataCommand { get; }

        public LibrarySettingsViewModel(ISettingsService settingsService,
            IIgdbService igdbService, MoodexState moodexState)
        {
            _settingsService = settingsService;
            _igdbService = igdbService;
            _moodexState = moodexState;
            var cfg = _settingsService.Load();
            _model = cfg;
            _igdbEnabled = cfg.Igdb.Enabled;

            // Consoles setup
            Consoles = new ObservableCollection<ConsoleInfo>(cfg.Consoles
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase));
            AddConsoleCommand = new RelayCommand(OnAddConsole);
            EditConsoleCommand = new RelayCommand(OnEditConsole, () => SelectedConsole != null);
            RemoveConsoleCommand = new RelayCommand(OnRemoveConsole, () => SelectedConsole != null);

            // Genres setup (mirror consoles)
            Genres = new ObservableCollection<string>(cfg.Genres
                .OrderBy(g => g, StringComparer.OrdinalIgnoreCase));
            AddGenreCommand = new RelayCommand(OnAddGenre);
            EditGenreCommand = new RelayCommand(OnEditGenre, () => SelectedGenre != null);
            RemoveGenreCommand = new RelayCommand(OnRemoveGenre, () => SelectedGenre != null);

            ScanForMissingDataCommand = new AsyncRelayCommand(ScanForMissingDataAsync);
        }

        // ─── Consoles handlers (unchanged) ───────────────────────────────
        private void OnAddConsole()
        {
            var dlg = new Moodex.Views.Utilities.ConsoleEditDialog();
            if (dlg.ShowDialog() != true) return;

            var newInfo = new ConsoleInfo
            {
                Id = dlg.ConsoleId,
                Name = dlg.ConsoleName,
                CoverAspectW = dlg.AspectW,
                CoverAspectH = dlg.AspectH
            };
            Consoles.Add(newInfo);
            SortCollections();
            SaveSettings();
        }

        private void OnEditConsole()
        {
            if (SelectedConsole == null) return;
            var ci = SelectedConsole;
            var dlg = new Moodex.Views.Utilities.ConsoleEditDialog
            {
                ConsoleId = ci.Id,
                ConsoleName = ci.Name,
                AspectW = ci.CoverAspectW,
                AspectH = ci.CoverAspectH
            };
            if (dlg.ShowDialog() != true) return;

            ci.Id = dlg.ConsoleId;
            ci.Name = dlg.ConsoleName;
            ci.CoverAspectW = dlg.AspectW;
            ci.CoverAspectH = dlg.AspectH;
            var idx = Consoles.IndexOf(ci);
            Consoles[idx] = ci;
            SortCollections();
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
            SortCollections();
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
            SortCollections();
            SaveSettings();
        }

        private void OnRemoveGenre()
        {
            if (SelectedGenre == null) return;
            Genres.Remove(SelectedGenre);
            SelectedGenre = null;
            SaveSettings();
        }

        // ─── IGDB Library Scan ──────────────────────────────────────────────

        private async Task ScanForMissingDataAsync()
        {
            if (!IgdbEnabled)
            {
                IgdbStatusMessage = "Enable IGDB integration first.";
                return;
            }

            IgdbStatusMessage = "Scanning library for missing data...";

            var games = _moodexState.Games.ToList();
            var candidates = new List<(GameInfo game, bool missingData, bool missingCover)>();

            foreach (var game in games)
            {
                bool missingGenres = string.IsNullOrWhiteSpace(game.Genre);
                bool missingDate = game.ReleaseDate.Year <= 1;
                bool missingCover = string.IsNullOrEmpty(GameCoverLocator.FindGameCover(game));

                if (missingGenres || missingDate || missingCover)
                    candidates.Add((game, missingGenres || missingDate, missingCover));
            }

            if (candidates.Count == 0)
            {
                IgdbStatusMessage = "All games have complete data.";
                return;
            }

            IgdbStatusMessage = $"Found {candidates.Count} game(s) with gaps. Starting review...";
            var owner = System.Windows.Application.Current.Windows
                .OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive);

            int dataApplied = 0, coversApplied = 0, reviewed = 0;

            foreach (var (game, missingData, missingCover) in candidates)
            {
                reviewed++;
                var root = game.GameRootPath;
                if (string.IsNullOrEmpty(root)) continue;

                IgdbStatusMessage = $"[{reviewed}/{candidates.Count}] Searching IGDB for \"{game.Name}\"...";

                IgdbGameResult? match;
                try
                {
                    var results = await _igdbService.SearchGameAsync(game.Name);
                    match = results.FirstOrDefault(r =>
                        string.Equals(r.Name, game.Name, StringComparison.OrdinalIgnoreCase))
                        ?? results.FirstOrDefault();
                }
                catch { continue; }

                if (match == null) continue;

                if (missingData && (match.Genres.Count > 0 || match.ReleaseDate.HasValue))
                {
                    IgdbStatusMessage = $"[{reviewed}/{candidates.Count}] Review data for \"{game.Name}\"";
                    var preview = new IgdbPreviewDataDialog(match);
                    if (owner != null) preview.Owner = owner;
                    preview.Title = $"IGDB Data: {game.Name}";

                    if (preview.ShowDialog() == true)
                    {
                        var manifestPath = Path.Combine(root, ".moodex_game");
                        GameManifest man;
                        if (File.Exists(manifestPath))
                        {
                            var json = File.ReadAllText(manifestPath);
                            man = JsonSerializer.Deserialize<GameManifest>(json) ?? new GameManifest();
                        }
                        else
                        {
                            man = new GameManifest();
                        }

                        bool changed = false;
                        if ((man.Genres == null || man.Genres.Count == 0) && match.Genres.Count > 0)
                        {
                            man.Genres = match.Genres;
                            game.Genre = string.Join(", ", match.Genres);
                            changed = true;
                        }
                        if ((man.ReleaseDateTime == null || man.ReleaseDateTime.Value.Year <= 1) && match.ReleaseDate.HasValue)
                        {
                            man.ReleaseDateTime = match.ReleaseDate.Value;
                            game.ReleaseDate = match.ReleaseDate.Value;
                            changed = true;
                        }
                        if (changed)
                        {
                            File.WriteAllText(manifestPath, JsonSerializer.Serialize(man, new JsonSerializerOptions { WriteIndented = true }));
                            dataApplied++;
                        }
                    }
                }

                if (missingCover && !string.IsNullOrEmpty(match.CoverImageId))
                {
                    IgdbStatusMessage = $"[{reviewed}/{candidates.Count}] Downloading cover for \"{game.Name}\"...";
                    try
                    {
                        var coverBytes = await _igdbService.DownloadCoverAsync(match.CoverImageId);
                        if (coverBytes != null && coverBytes.Length > 0)
                        {
                            var coverPreview = new IgdbPreviewCoverDialog(coverBytes);
                            if (owner != null) coverPreview.Owner = owner;
                            coverPreview.Title = $"IGDB Cover: {game.Name}";

                            if (coverPreview.ShowDialog() == true)
                            {
                                foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
                                {
                                    var old = Path.Combine(root, "cover" + ext);
                                    try { if (File.Exists(old)) File.Delete(old); } catch { }
                                }
                                var dest = Path.Combine(root, "cover.jpg");
                                File.WriteAllBytes(dest, coverBytes);
                                FreezingBitmapConverter.Invalidate(dest);
                                game.NotifyCoverChanged();
                                coversApplied++;
                            }
                        }
                    }
                    catch { }
                }
            }

            IgdbStatusMessage = $"Done. Applied data to {dataApplied} game(s), covers to {coversApplied} game(s).";
        }

        // ─── Persist both lists in one shot ──────────────────────────────
        private void SaveSettings()
        {
            var cfg = _settingsService.Load();
            cfg.Consoles = Consoles.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
            cfg.Genres = Genres.OrderBy(g => g, StringComparer.OrdinalIgnoreCase).ToList();
            _settingsService.Save(cfg);
        }

        private void SortCollections()
        {
            // Consoles
            var sortedConsoles = Consoles.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
            if (!Consoles.SequenceEqual(sortedConsoles))
            {
                Consoles.Clear();
                foreach (var c in sortedConsoles) Consoles.Add(c);
            }
            // Genres
            var sortedGenres = Genres.OrderBy(g => g, StringComparer.OrdinalIgnoreCase).ToList();
            if (!Genres.SequenceEqual(sortedGenres))
            {
                Genres.Clear();
                foreach (var g in sortedGenres) Genres.Add(g);
            }
        }

        // ─── INotifyPropertyChanged ─────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}

