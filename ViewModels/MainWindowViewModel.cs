using CommunityToolkit.Mvvm.Input;
using Moodex.Converters;
using Moodex.Models;
using Moodex.Services;
using Moodex.Views.Utilities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Moodex.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        // ──── File Paths ─────────────────────────────────────────────────────
        private readonly string _basePath = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string _dataFolder;
        private readonly string _gamesFile;
        private readonly string _emuFile;
        private readonly MoodexState _moodexState;

        // ──── Services ───────────────────────────────────────────────────────
        private readonly IDialogService _dialogs;
        private readonly IWindowPlacementService _placer;
        private readonly ISettingsService _settings;
        private readonly IArchiveService _archiver;
        private readonly IAutoHotKeyScriptService _scriptService;
        private readonly Dictionary<int, Process> _trackedGameProcesses = new();

        // ──── Exposed Collections & Views ──────────────────────────────────
        public ObservableCollection<GameInfo> Games => _moodexState.Games;
        public ObservableCollection<EmulatorInfo> Emulators => _moodexState.Emulators;
        public ICollectionView GamesView { get; }
        public ICollectionView? DisplayView { get; private set; }

        // ──── Selection ────────────────────────────────────────────────────
        private GameInfo? _selectedGame;
        public GameInfo? SelectedGame
        {
            get => _selectedGame;
            set
            {
                if (_selectedGame == value) return;
                _selectedGame = value;
                RaisePropertyChanged();

                // Enable/disable commands based on whether a game is selected
                RunCommand.NotifyCanExecuteChanged();
                EditGameCommand.NotifyCanExecuteChanged();
                DeleteGameCommand.NotifyCanExecuteChanged();
            }
        }

        // ──── Search & Grouping ─────────────────────────────────────────────
        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                RaisePropertyChanged();
                ApplyFilter();    // update view filter
            }
        }

        private string _groupBy = "Console";
        public string GroupBy
        {
            get => _groupBy;
            set
            {
                if (_groupBy == value) return;
                _groupBy = value;
                RaisePropertyChanged();
                ApplyGrouping();  // update grouping
            }
        }

        private bool _showArchived = true;
        public bool ShowArchived
        {
            get => _showArchived;
            set
            {
                if (_showArchived == value) return;
                _showArchived = value;
                RaisePropertyChanged();
                ApplyFilter();
            }
        }

        // ──── Commands ─────────────────────────────────────────────────────
        public IRelayCommand RunCommand { get; }
        public IRelayCommand AddGameCommand { get; }
        public IRelayCommand EditGameCommand { get; }
        public IRelayCommand DeleteGameCommand { get; }
        public IRelayCommand AddEmulatorCommand { get; }
        public IRelayCommand ShowManageEmulatorsCommand { get; }
        public IRelayCommand ShowSettingsCommand { get; }
        public IRelayCommand ShowAboutCommand { get; }
        public IAsyncRelayCommand ArchiveGameCommand { get; }
        public IAsyncRelayCommand ActivateGameCommand { get; }
        public IRelayCommand ClearSearchCommand { get; }

        // AutoHotKey Script Commands
        public IRelayCommand CreateScriptCommand { get; }
        public IRelayCommand EditScriptCommand { get; }
        public IRelayCommand DeleteScriptCommand { get; }
        public IRelayCommand ToggleScriptEnabledByNameCommand { get; }
        public IRelayCommand EditScriptByNameCommand { get; }
        public IRelayCommand DeleteScriptByNameCommand { get; }
        // Controller submenu
        public IRelayCommand ToggleControllerCommand { get; }
        public IRelayCommand ConfigureControllerProfileCommand { get; }
        public IRelayCommand DeleteControllerProfileCommand { get; }
        public IRelayCommand OpenGameFolderCommand { get; }
        // Completion toggles
        public IRelayCommand ToggleCompletedAnyPercentCommand { get; }
        public IRelayCommand ToggleCompletedMaxDifficultyCommand { get; }
        public IRelayCommand ToggleCompletedHundredPercentCommand { get; }
        // ──── Processing Banner ─────────────────────────────────────────────
        private string _processingBannerText = "";
        public string ProcessingBannerText
        {
            get => _processingBannerText;
            set { if (_processingBannerText != value) { _processingBannerText = value; RaisePropertyChanged(); } }
        }


        // ──── Constructor ──────────────────────────────────────────────────
        public MainWindowViewModel(IDialogService dialogs, IWindowPlacementService placer, ISettingsService settings, IAutoHotKeyScriptService scriptService, IArchiveService archiver, MoodexState moodexState)
        {
            _dialogs = dialogs;
            _placer = placer;
            _settings = settings;
            _scriptService = scriptService;
            _archiver = archiver;
            _dataFolder = Path.Combine(_basePath, "Data");
            _gamesFile = Path.Combine(_dataFolder, "games.json");
            _emuFile = Path.Combine(_dataFolder, "emulators.json");

            // load model via DI scanner-initialized MoodexState
            _moodexState = moodexState;

            // Initialize GroupBy from settings if available
            try
            {
                var cfg = _settings.Load();
                if (!string.IsNullOrWhiteSpace(cfg.DefaultGroupBy))
                {
                    _groupBy = cfg.DefaultGroupBy;
                    RaisePropertyChanged(nameof(GroupBy));
                }
            }
            catch { }

            // IsInArchive is already populated by the scanner from manifests

            // create view for grouping & filtering
            GamesView = CollectionViewSource.GetDefaultView(Games);
            DisplayView = GamesView;
            ApplyGrouping();
            ApplyFilter();

            // wire up commands
            RunCommand = new RelayCommand<GameInfo>(RunGame, game => game is not null);
            AddGameCommand = new RelayCommand(ExecuteAddGame);
            EditGameCommand = new RelayCommand<GameInfo>(ExecuteEditGame, game => game is not null);
            DeleteGameCommand = new RelayCommand<GameInfo>(ExecuteDeleteGame, game => game is not null);
            AddEmulatorCommand = new RelayCommand(ExecuteAddEmulator);
            ShowManageEmulatorsCommand = new RelayCommand(
                () => _dialogs.ShowManageEmulators()
            );
            ShowSettingsCommand = new RelayCommand<string?>(section =>
            {
                if (!string.IsNullOrWhiteSpace(section))
                {
                    _dialogs.ShowSettings(section);
                    // Refresh script commands after settings dialog closes
                    RefreshScriptCommands();
                }
            });
            ShowAboutCommand = new RelayCommand(ExecuteShowAbout);
            ArchiveGameCommand = new AsyncRelayCommand<GameInfo>(g => MoveGameAsync(g, toArchive: true), CanArchiveGame);
            ActivateGameCommand = new AsyncRelayCommand<GameInfo>(g => MoveGameAsync(g, toArchive: false), CanActivateGame);
            ClearSearchCommand = new RelayCommand(() => { SearchText = ""; });

            // AutoHotKey Script Commands
            CreateScriptCommand = new RelayCommand<GameInfo>(ExecuteCreateScript, CanCreateScript);
            EditScriptCommand = new RelayCommand<GameInfo>(ExecuteEditScript, CanEditScript);
            DeleteScriptCommand = new RelayCommand<GameInfo>(ExecuteDeleteScript, CanDeleteScript);
            ToggleScriptEnabledByNameCommand = new RelayCommand<string>(param => ExecuteToggleScriptByName(param));
            EditScriptByNameCommand = new RelayCommand<string>(param => ExecuteEditScriptByName(param));
            DeleteScriptByNameCommand = new RelayCommand<string>(param => ExecuteDeleteScriptByName(param));
            ToggleControllerCommand = new RelayCommand<GameInfo>(ExecuteToggleController);
            ConfigureControllerProfileCommand = new RelayCommand<GameInfo>(ExecuteConfigureControllerProfile);
            DeleteControllerProfileCommand = new RelayCommand<GameInfo>(ExecuteDeleteControllerProfile, CanDeleteControllerProfile);
            OpenGameFolderCommand = new RelayCommand<GameInfo>(ExecuteOpenGameFolder, g => g != null);
            // Completion
            ToggleCompletedAnyPercentCommand = new RelayCommand<GameInfo>(g => ToggleCompletion(g, which: 1));
            ToggleCompletedMaxDifficultyCommand = new RelayCommand<GameInfo>(g => ToggleCompletion(g, which: 2));
            ToggleCompletedHundredPercentCommand = new RelayCommand<GameInfo>(g => ToggleCompletion(g, which: 3));
        }

        // ──── Actions ───────────────────────────────────────────────────────
        private void RunGame(GameInfo? game)
        {
            if (game == null) return;
            try
            {
                // Check if the game is archived as a zip file
                var settings = _settings.Load();
                var installRoot = GetInstallRoot(game, settings.ActiveLibraryPath, settings.ArchiveLibraryPath);
                if (installRoot.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        "This game is archived and compressed. Please move it to Active storage before launching.",
                        "Game Archived",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Launch DS4Windows if enabled for this game
                if (settings.IsDs4Installed && game.ControllerEnabled)
                {
                    // If a per-game DS4 profile exists, copy it into DS4Windows Default.xml before launch
                    try
                    {
                        if (!string.IsNullOrEmpty(game.GameRootPath))
                        {
                            var perGame = Path.Combine(game.GameRootPath, "input", "ds4windows_controller_profile.xml");
                            if (File.Exists(perGame))
                            {
                                var baseDir = Path.Combine(AppContext.BaseDirectory, "External Tools", "DS4Windows");
                                var profilesDir = Path.Combine(baseDir, "Profiles");
                                Directory.CreateDirectory(profilesDir);
                                var defaultXml = Path.Combine(profilesDir, "Default.xml");
                                File.Copy(perGame, defaultXml, overwrite: true);
                            }
                        }
                    }
                    catch { }
                    TryLaunchDs4Windows();
                }

                ProcessStartInfo startInfo;

                // If the game is on PC, launch the executable directly
                if (game.ConsoleId == "pc")
                {
                    var exe = game.FileSystemPath;
                    startInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        WorkingDirectory = Path.GetDirectoryName(exe)!,
                        UseShellExecute = false
                    };
                }
                else
                {
                    // Otherwise, find the one emulator configured for that console
                    var emulator = Emulators
                        .FirstOrDefault(e => e.EmulatedConsoleId == game.ConsoleId);

                    if (emulator == null)
                    {
                        MessageBox.Show(
                            $"No emulator configured for {game.ConsoleName}.",
                            "Launch Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Pass the single FileSystemPath to the emulator, substituting any placeholder
                    var target = game.FileSystemPath;
                    string args;

                    if (string.IsNullOrWhiteSpace(emulator.DefaultArguments))
                    {
                        args = $"\"{target}\"";
                    }
                    else
                    {
                        args = emulator.DefaultArguments
                            .Replace("{RomPath}", target)
                            .Replace("{FolderPath}", target);
                    }

                    startInfo = new ProcessStartInfo
                    {
                        FileName = emulator.ExecutablePath,
                        Arguments = args,
                        UseShellExecute = true
                    };
                }

                // Start the process
                var process = Process.Start(startInfo);
                if (process != null)
                {
                    // Launch AutoHotKey script if this console is AHK-enabled and a script exists
                    if (IsAutoHotKeyInstalled() && _scriptService.HasScript(game))
                    {
                        _scriptService.LaunchScript(game);
                    }

                    // Track the process for cleanup when it exits
                    TrackGameProcess(process, game);

                    // Position its window on the user's chosen monitor
                    var screens = Screen.AllScreens;
                    int idx = settings.DefaultMonitorIndex;
                    var primaryScreen = Screen.PrimaryScreen ?? screens[0];
                    var targetScreen = (idx >= 0 && idx < screens.Length)
                                       ? screens[idx]
                                       : primaryScreen;

                    _placer.PlaceProcessWindows(process, targetScreen, primaryScreen);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to launch game:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ToggleCompletion(GameInfo? game, int which)
        {
            if (game == null) return;
            switch (which)
            {
                case 1:
                    UpdateGameManifest(game, m => m.CompletionAnyPercent = game.CompletedAnyPercent);
                    break;
                case 2:
                    UpdateGameManifest(game, m => m.CompletionMaxDifficulty = game.CompletedMaxDifficulty);
                    break;
                case 3:
                    UpdateGameManifest(game, m => m.CompletionHundredPercent = game.CompletedHundredPercent);
                    break;
            }
            // No view refresh needed; bindings update via INPC on GameInfo properties
        }
        private void ExecuteAddGame()
        {
            // show dialog and get new game
            var newGame = _dialogs.ShowAddGame();
            if (newGame == null) return;

            Games.Add(newGame);
            SaveGames();
        }

        private void ExecuteEditGame(GameInfo? game)
        {
            if (game == null) return;
            // show edit dialog; returns null if cancelled
            var edited = _dialogs.ShowEditGame(game);
            if (edited == null) return;

            SaveGames();
            GamesView.Refresh();
        }

        private void ExecuteDeleteGame(GameInfo? game)
        {
            if (game == null) return;
            // strong confirmation
            var confirmText =
                $"You are about to permanently delete this game and all related data.\n\n" +
                $"Game: {game.Name}\n" +
                $"Console: {game.ConsoleName}\n\n" +
                "This will delete:\n" +
                "- Game files and folders\n" +
                "- AutoHotKey scripts\n" +
                "- Controller configurations\n" +
                "- Recorded Achievements\n" +
                "- Archived game data (if archived)\n\n" +
                "Are you sure you want to proceed?";

            if (System.Windows.MessageBox.Show(confirmText, "Confirm Delete",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                // delete active game folder if present
                if (!string.IsNullOrEmpty(game.GameRootPath) && Directory.Exists(game.GameRootPath))
                {
                    try { Directory.Delete(game.GameRootPath, recursive: true); }
                    catch
                    {
                        // try once more after a small delay
                        System.Threading.Thread.Sleep(300);
                        try { Directory.Delete(game.GameRootPath, recursive: true); } catch { }
                    }
                }

                // delete archived zip/folder by GUID if present
                var settings = _settings.Load();
                if (!string.IsNullOrEmpty(game.GameGuid) && !string.IsNullOrEmpty(settings.ArchiveLibraryPath))
                {
                    var basePath = Path.Combine(settings.ArchiveLibraryPath, "Game Data");
                    var zip = Path.Combine(basePath, game.GameGuid + ".zip");
                    var folder = Path.Combine(basePath, game.GameGuid);
                    try { if (File.Exists(zip)) File.Delete(zip); } catch { }
                    if (Directory.Exists(folder))
                    {
                        try { Directory.Delete(folder, recursive: true); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete game files:\n{ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // remove from list regardless (UI reflects best-effort delete)
            Games.Remove(game);
            SaveGames();
            GamesView.Refresh();
        }

        private void ExecuteOpenGameFolder(GameInfo? game)
        {
            if (game == null) return;
            try
            {
                string? folder = null;
                if (!string.IsNullOrEmpty(game.GameRootPath) && Directory.Exists(game.GameRootPath))
                {
                    folder = game.GameRootPath;
                }
                else if (!string.IsNullOrEmpty(game.FileSystemPath))
                {
                    var dir = Path.GetDirectoryName(game.FileSystemPath);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) folder = dir;
                }
                if (folder != null)
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
                }
            }
            catch { }
        }

        private void ExecuteAddEmulator()
        {
            // show add-emulator dialog
            var newEmu = _dialogs.ShowAddEmulator();
            if (newEmu == null) return;

            Emulators.Add(newEmu);
            SaveEmulators();
        }

        private void ExecuteShowAbout()
        {
            // Delegate to your dialog service
            _dialogs.ShowAbout();
        }

        // ──── Persistence (legacy JSON disabled) ────────────────────────────
        private void SaveGames() { /* manifest-driven; no JSON save */ }
        private void SaveEmulators() { /* manifest-driven; no JSON save */ }

        // ──── Filter & Group ───────────────────────────────────────────────
        private void ApplyFilter()
        {
            GamesView.Filter = o =>
            {
                if (o is GameInfo g)
                {
                    // if “ShowArchived” is false, hide archived games
                    if (!ShowArchived && g.IsInArchive)
                        return false;

                    // then your existing search‐text filter
                    if (!string.IsNullOrWhiteSpace(SearchText)
                     && !g.Name.Contains(SearchText,
                                          StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                return true;
            };
            GamesView.Refresh();
            ApplyDisplayFilter();
        }

        private void ApplyDisplayFilter()
        {
            if (DisplayView == null || DisplayView == GamesView) return;
            DisplayView.Filter = o =>
            {
                if (o is GenreGameItem gi)
                {
                    var g = gi.Game;
                    if (!ShowArchived && g.IsInArchive) return false;
                    if (!string.IsNullOrWhiteSpace(SearchText)
                        && !g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) return false;
                }
                return true;
            };
            DisplayView.Refresh();
        }

        private void ApplyGrouping()
        {
            var consoles = GamesView
                .Cast<GameInfo>()
                .Select(g => g.ConsoleName ?? "(null)")
                .Distinct();

            Debug.WriteLine("Unique consoles: " + string.Join(", ", consoles));

            // Reset to the base view by default
            DisplayView = GamesView;
            RaisePropertyChanged(nameof(DisplayView));

            GamesView.GroupDescriptions.Clear();
            GamesView.SortDescriptions.Clear();

            if (string.Equals(GroupBy, "Genre", StringComparison.OrdinalIgnoreCase))
            {
                var items = Games
                    .SelectMany(g => SplitGenres(g.Genre)
                        .Select(genre => new GenreGameItem { Game = g, Genre = genre }))
                    .ToList();

                var lv = new ListCollectionView(items);
                lv.GroupDescriptions.Clear();
                lv.SortDescriptions.Clear();
                lv.GroupDescriptions.Add(new PropertyGroupDescription(nameof(GenreGameItem.Genre)));
                lv.SortDescriptions.Add(new SortDescription(nameof(GenreGameItem.Genre), ListSortDirection.Ascending));
                lv.SortDescriptions.Add(new SortDescription($"{nameof(GenreGameItem.Game)}.{nameof(GameInfo.Name)}", ListSortDirection.Ascending));
                DisplayView = lv;
                RaisePropertyChanged(nameof(DisplayView));
                ApplyDisplayFilter();
                DisplayView.Refresh();
            }
            else
            {
                // set group description based on selected criteria for base view
                PropertyGroupDescription? pgd = GroupBy switch
                {
                    "Console" => new PropertyGroupDescription(nameof(GameInfo.ConsoleName)),
                    "Year" => new PropertyGroupDescription(nameof(GameInfo.ReleaseYear)),
                    _ => null
                };

                if (pgd != null)
                {
                    GamesView.GroupDescriptions.Add(pgd);
                    GamesView.SortDescriptions.Add(new SortDescription(pgd.PropertyName, ListSortDirection.Ascending));
                    GamesView.SortDescriptions.Add(new SortDescription(nameof(GameInfo.Name), ListSortDirection.Ascending));
                }

                GamesView.Refresh();
                DisplayView = GamesView;
                RaisePropertyChanged(nameof(DisplayView));
            }
        }

        private static IEnumerable<string> SplitGenres(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) yield break;
            foreach (var s in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var t = s.Trim();
                if (!string.IsNullOrEmpty(t)) yield return t;
            }
        }

        public static string GetInstallRoot(GameInfo game,
                                    string activeRoot,
                                    string archiveRoot)
        {
            // 0) pick the path we actually know
            var path = game.FileSystemPath;
            if (string.IsNullOrEmpty(path))
                return "";

            // Special case: if the path is a zip file, return it as-is
            if (File.Exists(path) && path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            // Special case: if the path is a folder in the archive that contains a zip file
            // (this is the new compressed archive structure with cover images)
            if (Directory.Exists(path))
            {
                var folderName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                var zipInFolder = Path.Combine(path, $"{folderName}.zip");
                if (File.Exists(zipInFolder))
                {
                    return zipInFolder;
                }
            }

            // 1) start from the folder that *contains* the file (if any)
            string dir = File.Exists(path)
                       ? Path.GetDirectoryName(path)!
                       : path;

            // 2) walk up until the parent is the console folder (e.g. "Playstation 2")
            var consoleName = game.ConsoleName;
            while (true)
            {
                var parent = Path.GetDirectoryName(dir);
                if (parent == null)
                    break;

                // once we've reached <Console>\<GameName>, stop
                if (string.Equals(Path.GetFileName(parent),
                                  consoleName,
                                  StringComparison.OrdinalIgnoreCase))
                {
                    return dir;
                }

                // bail if we've climbed out of the library roots
                if (parent.Equals(activeRoot, StringComparison.OrdinalIgnoreCase) ||
                    parent.Equals(archiveRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return dir;
                }

                dir = parent;
            }

            // fallback
            return dir;
        }

        private async Task MoveGameAsync(GameInfo? game, bool toArchive)
        {
            if (game == null) return;

            var set = _settings.Load();
            // New model: archive/restore operates on data/ and guid-based zip path

            // show progress…
            var vm = new ProgressWindowViewModel
            {
                Title = toArchive ? "Moving Game to Archive" : "Moving Game to Active"
            };
            var dlg = new ProgressWindow
            {
                Owner = Application.Current.MainWindow,
                DataContext = vm
            };
            dlg.Show();
            // mark processing for tile overlay and banner
            game.IsProcessing = true;
            ProcessingBannerText = toArchive ? $"Archiving {game.Name} (0%)" : $"Restoring {game.Name} (0%)";
            var prog = new Progress<MoveProgress>(p =>
            {
                vm.Percent = p.Percent;
                vm.CurrentFile = p.CurrentFile;
                game.ProcessingPercent = p.Percent;
                ProcessingBannerText = toArchive ? $"Archiving {game.Name} ({p.Percent:F0}%)" : $"Restoring {game.Name} ({p.Percent:F0}%)";
            });

            bool success = false;
            try
            {
                if (toArchive)
                {
                    var res = await _archiver.ArchiveGameAsync(game, set.ArchiveLibraryPath, prog, vm.Token);
                    success = res.Success;
                    if (!success) throw new Exception(res.Message ?? "Archive failed");
                }
                else
                {
                    var res = await _archiver.RestoreGameAsync(game, set.ArchiveLibraryPath, prog, vm.Token);
                    success = res.Success;
                    if (!success) throw new Exception(res.Message ?? "Restore failed");
                }
            }
            catch (OperationCanceledException) { /* user hit Cancel */ }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to move game:\n{ex.Message}",
                                "Move Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { dlg.Close(); }

            if (!success)
            {
                game.IsProcessing = false;
                game.ProcessingPercent = 0;
                ProcessingBannerText = "";
                return;   // aborted or failed
            }

            // Update runtime fields
            game.IsInArchive = toArchive;
            if (!toArchive && !string.IsNullOrEmpty(game.GameRootPath) && !string.IsNullOrEmpty(game.LaunchTarget))
            {
                var dataDir = Path.Combine(game.GameRootPath, "data");
                game.FileSystemPath = Path.Combine(dataDir, game.LaunchTarget);
            }
            game.IsProcessing = false;
            game.ProcessingPercent = 0;
            ProcessingBannerText = "";
            GamesView.Refresh();
        }

        // ──── AutoHotKey Script Commands ─────────────────────────────────────

        private void ExecuteCreateScript(GameInfo? game)
        {
            if (game == null) return;
            try
            {
                var scriptPath = _scriptService.CreateScript(game);
                SaveGames();
                GamesView.Refresh();
                
                // Open the script for editing
                var name = Path.GetFileNameWithoutExtension(scriptPath);
                _scriptService.EditScript(game, name);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create script:\n{ex.Message}",
                    "Script Creation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteEditScript(GameInfo? game)
        {
            if (game == null) return;
            try
            {
                _scriptService.EditScript(game);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to edit script:\n{ex.Message}",
                    "Script Edit Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDeleteScript(GameInfo? game)
        {
            if (game == null) return;
            try
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete the AutoHotKey script for '{game.Name}'?",
                    "Delete Script", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _scriptService.DeleteScript(game);
                    SaveGames();
                    GamesView.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete script:\n{ex.Message}",
                    "Script Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanCreateScript(GameInfo? game)
        {
            return game != null && IsAutoHotKeyInstalled();
        }

        private bool CanEditScript(GameInfo? game)
        {
            return game != null && _scriptService.HasScript(game) && IsAutoHotKeyInstalled();
        }

        private bool CanDeleteScript(GameInfo? game)
        {
            return game != null && _scriptService.HasScript(game) && IsAutoHotKeyInstalled();
        }

        private bool IsAutoHotKeyInstalled()
        {
            var settings = _settings.Load();
            return settings.IsAutoHotKeyInstalled;
        }

        private bool CanArchiveGame(GameInfo? game)
        {
            if (game == null) return false;
            if (game.IsInArchive) return false;
            var s = _settings.Load();
            var root = game.GameRootPath;
            if (string.IsNullOrEmpty(root)) return false;
            var dataDir = Path.Combine(root, "data");
            // Avoid expensive recursive file enumeration on the UI thread.
            // Presence of the data directory is sufficient to enable archiving.
            return Directory.Exists(dataDir);
        }

        private bool CanActivateGame(GameInfo? game)
        {
            // Avoid touching archive storage on menu open; validate on click instead
            return game != null && game.IsInArchive && !string.IsNullOrEmpty(game.GameGuid);
        }

        // Console-level AHK enablement removed; AHK applies to all consoles when installed

        /// <summary>
        /// Public property to expose AutoHotKey installation status for XAML binding
        /// </summary>
        public bool AutoHotKeyInstalled => IsAutoHotKeyInstalled();

        /// <summary>
        /// Refreshes the AutoHotKey script command states
        /// Call this when returning from settings to update command availability
        /// </summary>
        public void RefreshScriptCommands()
        {
            CreateScriptCommand.NotifyCanExecuteChanged();
            EditScriptCommand.NotifyCanExecuteChanged();
            DeleteScriptCommand.NotifyCanExecuteChanged();
            RaisePropertyChanged(nameof(AutoHotKeyInstalled));
        }

        /// <summary>
        /// Tracks a game process and sets up cleanup when it exits
        /// </summary>
        /// <param name="process">The game process to track</param>
        /// <param name="game">The game being launched</param>
        private void TrackGameProcess(Process process, GameInfo game)
        {
            // Store the process for cleanup
            _trackedGameProcesses[process.Id] = process;

            // Set up cleanup when the process exits
            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) => OnGameProcessExited(process, game);
        }

        /// <summary>
        /// Handles cleanup when a game process exits
        /// </summary>
        /// <param name="process">The exited game process</param>
        /// <param name="game">The game that was running</param>
        private void OnGameProcessExited(Process process, GameInfo game)
        {
            try
            {
                // Remove from tracking
                _trackedGameProcesses.Remove(process.Id);

                // Clean up AutoHotKey scripts for this game
                if (IsAutoHotKeyInstalled() && _scriptService.HasScript(game))
                {
                    CleanupAutoHotKeyScripts(game);
                }

                // Close DS4Windows if it was enabled
                var settings = _settings.Load();
                if (settings.IsDs4Installed && game.ControllerEnabled)
                {
                    TryCloseDs4Windows();
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't show to user as this happens in background
                System.Diagnostics.Debug.WriteLine($"Error during game cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up AutoHotKey scripts for a specific game
        /// </summary>
        /// <param name="game">The game to clean up scripts for</param>
        private void CleanupAutoHotKeyScripts(GameInfo game)
        {
            try
            {
                // Find and terminate any AutoHotKey processes that might be running scripts for this game
                var runningAhkProcesses = Process.GetProcessesByName("AutoHotkey64")
                    .Concat(Process.GetProcessesByName("AutoHotkey"))
                    .Concat(Process.GetProcessesByName("AutoHotkeyU64"))
                    .Concat(Process.GetProcessesByName("AutoHotkeyU32"))
                    .ToList();

                foreach (var ahkProcess in runningAhkProcesses)
                {
                    try
                    {
                        // Check if this AHK process is running a script for our game
                        if (IsAutoHotKeyProcessForGame(ahkProcess, game))
                        {
                            ahkProcess.Kill();
                            ahkProcess.WaitForExit(5000); // Wait up to 5 seconds for graceful shutdown
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error terminating AHK process {ahkProcess.Id}: {ex.Message}");
                    }
                    finally
                    {
                        ahkProcess.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during AHK cleanup for {game.Name}: {ex.Message}");
            }
        }

        // --- Script menu helpers ---
        private (GameInfo? game, string? name) ParseScriptParam(string? param)
        {
            if (string.IsNullOrWhiteSpace(param)) return (null, null);
            var parts = param.Split('|');
            if (parts.Length != 2) return (null, null);
            var guid = parts[0];
            var name = parts[1];
            var game = Games.FirstOrDefault(g => string.Equals(g.GameGuid, guid, StringComparison.OrdinalIgnoreCase));
            return (game, name);
        }

        private void ExecuteToggleScriptByName(string? param)
        {
            var (game, name) = ParseScriptParam(param);
            if (game == null || string.IsNullOrWhiteSpace(name)) return;
            var script = game.InputScripts.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (script == null) return;
            var newEnabled = !script.Enabled;
            _scriptService.SetScriptEnabled(game, name, newEnabled);
            script.Enabled = newEnabled;
        }

        private void ExecuteEditScriptByName(string? param)
        {
            var (game, name) = ParseScriptParam(param);
            if (game == null || string.IsNullOrWhiteSpace(name)) return;
            _scriptService.EditScript(game, name);
        }

        private void ExecuteDeleteScriptByName(string? param)
        {
            var (game, name) = ParseScriptParam(param);
            if (game == null || string.IsNullOrWhiteSpace(name)) return;
            _scriptService.DeleteScript(game, name);
            var s = game.InputScripts.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (s != null) game.InputScripts.Remove(s);
            game.HasAutoHotKeyScript = game.InputScripts.Count > 0;
        }

        private void ExecuteToggleController(GameInfo? game)
        {
            if (game == null) return;
            game.ControllerEnabled = !game.ControllerEnabled;
            UpdateGameManifest(game, man => man.ControllerEnabled = game.ControllerEnabled);
        }

        private void ExecuteConfigureControllerProfile(GameInfo? game)
        {
            if (game == null) return;
            try
            {
                var win = new Moodex.Views.Utilities.ControllerProfileSetupWindow(game)
                {
                    Owner = Application.Current.MainWindow
                };
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open controller setup window:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanDeleteControllerProfile(GameInfo? game)
        {
            // Use manifest-driven flag to avoid filesystem checks on menu open
            return game != null && game.ControllerProfileConfigured;
        }

        private void ExecuteDeleteControllerProfile(GameInfo? game)
        {
            if (game == null) return;
            try
            {
                if (!string.IsNullOrEmpty(game.GameRootPath))
                {
                    var path = Path.Combine(game.GameRootPath, "input", "ds4windows_controller_profile.xml");
                    if (File.Exists(path)) File.Delete(path);
                }
                game.ControllerProfileConfigured = false;
                UpdateGameManifest(game, man => man.ControllerProfileConfigured = false);

                // Reset DS4Windows default profile to seeded default
                var baseDir = Path.Combine(AppContext.BaseDirectory, "External Tools", "DS4Windows");
                var initializer = new Moodex.Utilities.DS4WindowsInitializer(baseDir);
                initializer.Initialize();

                DeleteControllerProfileCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete controller profile:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TryLaunchDs4Windows()
        {
            try
            {
                var baseDir = Path.Combine(AppContext.BaseDirectory, "External Tools", "DS4Windows");
                if (!Directory.Exists(baseDir)) return;
                var exe = Directory.GetFiles(baseDir, "DS4Windows.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (exe == null) return;
                Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
            }
            catch { }
        }

        private void TryCloseDs4Windows()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("DS4Windows"))
                {
                    try
                    {
                        p.CloseMainWindow();
                        if (!p.WaitForExit(2000)) p.Kill();
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void UpdateGameManifest(GameInfo game, Action<Moodex.Models.Manifests.GameManifest> update)
        {
            if (string.IsNullOrEmpty(game.GameRootPath)) return;
            var path = Path.Combine(game.GameRootPath, ".moodex_game");
            Moodex.Models.Manifests.GameManifest man;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                man = System.Text.Json.JsonSerializer.Deserialize<Moodex.Models.Manifests.GameManifest>(json) ?? new Moodex.Models.Manifests.GameManifest();
            }
            else
            {
                man = new Moodex.Models.Manifests.GameManifest { Name = game.Name, Guid = game.GameGuid ?? Guid.NewGuid().ToString(), ConsoleId = game.ConsoleId };
            }
            update(man);
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(man, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }

        /// <summary>
        /// Determines if an AutoHotKey process is running a script for the specified game
        /// </summary>
        /// <param name="ahkProcess">The AutoHotKey process to check</param>
        /// <param name="game">The game to check against</param>
        /// <returns>True if the process is running a script for this game</returns>
        private bool IsAutoHotKeyProcessForGame(Process ahkProcess, GameInfo game)
        {
            try
            {
                // Get the command line of the AHK process
                var commandLine = GetProcessCommandLine(ahkProcess.Id);
                
                // Check if the command line references any script from the game's input folder
                if (!string.IsNullOrEmpty(game.GameRootPath))
                {
                    var inputFolder = System.IO.Path.Combine(game.GameRootPath, "input");
                    if (commandLine.Contains(inputFolder, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch
            {
                // If we can't determine the command line, assume it might be for this game
                // This is a conservative approach to ensure cleanup
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the command line of a process by its ID
        /// </summary>
        /// <param name="processId">The process ID</param>
        /// <returns>The command line string</returns>
        private string GetProcessCommandLine(int processId)
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
                
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    return obj["CommandLine"]?.ToString() ?? string.Empty;
                }
            }
            catch
            {
                // If WMI fails, return empty string
            }

            return string.Empty;
        }

    }
}
