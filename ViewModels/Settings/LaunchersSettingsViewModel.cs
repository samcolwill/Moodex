using CommunityToolkit.Mvvm.Input;
using Moodex.Configuration;
using Moodex.Services;
using Moodex.Services.Steam;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Moodex.ViewModels.Settings
{
    public class LaunchersSettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _svc;
        private readonly SettingsModel _model;
        private readonly ISteamDetectionService _steamDetection;
        private readonly ISteamImportService _steamImport;
        private readonly ISteamWebApiService _steamWebApi;

        public LaunchersSettingsViewModel(ISettingsService svc, ISteamDetectionService steamDetection,
            ISteamImportService steamImport, ISteamWebApiService steamWebApi)
        {
            _svc = svc;
            _model = _svc.Load();
            _steamDetection = steamDetection;
            _steamImport = steamImport;
            _steamWebApi = steamWebApi;

            _autoScanOnStartup = _model.Steam.AutoScanOnStartup;
            _steamApiKey = _model.Steam.SteamApiKey;
            _steamPath = _model.Steam.SteamExePath;

            var normalized = NormalizeLibraryPaths(_model.Steam.LibraryPaths);
            _model.Steam.LibraryPaths = normalized;
            SteamLibraryPaths = new ObservableCollection<string>(normalized);
            IgnoredGames = new ObservableCollection<string>(_model.Steam.IgnoredGames);

            ConfigureSteamCommand = new AsyncRelayCommand(ConfigureSteamAsync);
            ScanLibraryCommand = new AsyncRelayCommand(ScanLibraryAsync);
            RemoveLibraryPathCommand = new RelayCommand(RemoveSelectedLibraryPath, () => SelectedLibraryPath != null);
            RemoveIgnoredGameCommand = new RelayCommand(RemoveSelectedIgnoredGame, () => SelectedIgnoredGame != null);
        }

        // ── Properties ──────────────────────────────────────────────────────

        private string _steamApiKey = string.Empty;
        public string SteamApiKey
        {
            get => _steamApiKey;
            set
            {
                if (_steamApiKey != value)
                {
                    _steamApiKey = value;
                    _model.Steam.SteamApiKey = value;
                    _svc.Save(_model);
                    OnPropertyChanged();
                }
            }
        }

        private string _steamPath = string.Empty;
        public string SteamPath
        {
            get => _steamPath;
            set
            {
                if (_steamPath != value)
                {
                    _steamPath = value;
                    _model.Steam.SteamExePath = value;
                    _svc.Save(_model);
                    OnPropertyChanged();
                }
            }
        }

        private bool _autoScanOnStartup;
        public bool AutoScanOnStartup
        {
            get => _autoScanOnStartup;
            set
            {
                if (_autoScanOnStartup != value)
                {
                    _autoScanOnStartup = value;
                    _model.Steam.AutoScanOnStartup = value;
                    _svc.Save(_model);
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> SteamLibraryPaths { get; }
        public ObservableCollection<string> IgnoredGames { get; }

        private string? _selectedLibraryPath;
        public string? SelectedLibraryPath
        {
            get => _selectedLibraryPath;
            set
            {
                if (_selectedLibraryPath != value)
                {
                    _selectedLibraryPath = value;
                    OnPropertyChanged();
                    RemoveLibraryPathCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private string? _selectedIgnoredGame;
        public string? SelectedIgnoredGame
        {
            get => _selectedIgnoredGame;
            set
            {
                if (_selectedIgnoredGame != value)
                {
                    _selectedIgnoredGame = value;
                    OnPropertyChanged();
                    RemoveIgnoredGameCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            private set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
        }

        // ── Commands ────────────────────────────────────────────────────────

        public IAsyncRelayCommand ConfigureSteamCommand { get; }
        public IAsyncRelayCommand ScanLibraryCommand { get; }
        public IRelayCommand RemoveLibraryPathCommand { get; }
        public IRelayCommand RemoveIgnoredGameCommand { get; }

        private async Task ConfigureSteamAsync()
        {
            if (string.IsNullOrWhiteSpace(SteamApiKey))
            {
                StatusMessage = "Enter your Steam API key first.";
                return;
            }

            StatusMessage = "Detecting Steam...";

            var installation = _steamDetection.DetectSteamInstallation();
            if (installation == null)
            {
                StatusMessage = "Steam installation not found.";
                return;
            }

            SteamPath = installation.SteamExePath;
            var normalizedPaths = NormalizeLibraryPaths(installation.LibraryPaths);
            _model.Steam.LibraryPaths = normalizedPaths;
            SteamLibraryPaths.Clear();
            foreach (var p in normalizedPaths)
                SteamLibraryPaths.Add(p);

            var steamRoot = Path.GetDirectoryName(installation.SteamExePath);
            if (!string.IsNullOrEmpty(steamRoot))
            {
                var detectedId = _steamDetection.DetectSteamId64(steamRoot);
                if (!string.IsNullOrEmpty(detectedId))
                    _model.Steam.SteamId64 = detectedId;
            }

            if (string.IsNullOrWhiteSpace(_model.Steam.SteamId64))
            {
                StatusMessage = "Could not detect your Steam ID. Check that you are logged in to Steam.";
                _svc.Save(_model);
                return;
            }

            StatusMessage = "Fetching owned games...";

            List<SteamGameEntry> ownedGames;
            try
            {
                ownedGames = await _steamWebApi.GetOwnedGamesAsync(SteamApiKey, _model.Steam.SteamId64);
            }
            catch (Exception ex)
            {
                StatusMessage = $"API error: {ex.Message}";
                _svc.Save(_model);
                return;
            }

            if (ownedGames.Count == 0)
            {
                StatusMessage = "API returned no games. Check that your profile game details are set to Public.";
                _svc.Save(_model);
                return;
            }

            StatusMessage = "Checking local install status...";

            var allGames = _steamDetection.ResolveInstallStatus(ownedGames, _model.Steam.LibraryPaths);
            var importResult = _steamImport.ImportSteamGames(_model.ActiveLibraryPath, allGames, steamRoot, _model.Steam.IgnoredGames);

            _model.Steam.Enabled = true;
            _svc.Save(_model);

            var installed = allGames.Count(g => g.IsInstalled);
            StatusMessage = $"Connected. {ownedGames.Count} owned, {installed} installed. " +
                            $"Imported {importResult.Imported}, removed {importResult.Removed}.";
        }

        private async Task ScanLibraryAsync()
        {
            if (string.IsNullOrWhiteSpace(_model.Steam.SteamApiKey) || string.IsNullOrWhiteSpace(_model.Steam.SteamId64))
            {
                StatusMessage = "Run Configure Steam first.";
                return;
            }

            if (_model.Steam.LibraryPaths.Count == 0)
            {
                StatusMessage = "No library folders configured.";
                return;
            }

            StatusMessage = "Fetching owned games...";

            List<SteamGameEntry> ownedGames;
            try
            {
                ownedGames = await _steamWebApi.GetOwnedGamesAsync(_model.Steam.SteamApiKey, _model.Steam.SteamId64);
            }
            catch (Exception ex)
            {
                StatusMessage = $"API error: {ex.Message}";
                return;
            }

            if (ownedGames.Count == 0)
            {
                StatusMessage = "API returned no games. Check that your profile game details are set to Public.";
                return;
            }

            StatusMessage = "Checking local install status...";

            var allGames = _steamDetection.ResolveInstallStatus(ownedGames, _model.Steam.LibraryPaths);
            var steamRoot = !string.IsNullOrEmpty(_model.Steam.SteamExePath)
                ? Path.GetDirectoryName(_model.Steam.SteamExePath) : null;
            var importResult = _steamImport.ImportSteamGames(_model.ActiveLibraryPath, allGames, steamRoot, _model.Steam.IgnoredGames);

            var installed = allGames.Count(g => g.IsInstalled);
            StatusMessage = $"Scan complete. {ownedGames.Count} owned, {installed} installed. " +
                            $"Imported {importResult.Imported}, removed {importResult.Removed}.";
        }

        private void RemoveSelectedLibraryPath()
        {
            if (SelectedLibraryPath == null) return;
            SteamLibraryPaths.Remove(SelectedLibraryPath);
            _model.Steam.LibraryPaths = new List<string>(SteamLibraryPaths);
            _svc.Save(_model);
            SelectedLibraryPath = null;
        }

        private void RemoveSelectedIgnoredGame()
        {
            if (SelectedIgnoredGame == null) return;
            IgnoredGames.Remove(SelectedIgnoredGame);
            _model.Steam.IgnoredGames = new List<string>(IgnoredGames);
            _svc.Save(_model);
            SelectedIgnoredGame = null;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static List<string> NormalizeLibraryPaths(List<string> paths)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            foreach (var p in paths)
            {
                try
                {
                    var norm = Path.GetFullPath(p);
                    if (seen.Add(norm))
                        result.Add(norm);
                }
                catch
                {
                    if (seen.Add(p))
                        result.Add(p);
                }
            }
            return result;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
