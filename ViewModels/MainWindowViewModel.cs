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

        // AutoHotKey Script Commands
        public IRelayCommand CreateScriptCommand { get; }
        public IRelayCommand EditScriptCommand { get; }
        public IRelayCommand DeleteScriptCommand { get; }
        public IRelayCommand ToggleScriptEnabledByNameCommand { get; }
        public IRelayCommand EditScriptByNameCommand { get; }
        public IRelayCommand DeleteScriptByNameCommand { get; }
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

            // IsInArchive is already populated by the scanner from manifests

            // create view for grouping & filtering
            GamesView = CollectionViewSource.GetDefaultView(Games);
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

            // AutoHotKey Script Commands
            CreateScriptCommand = new RelayCommand<GameInfo>(ExecuteCreateScript, CanCreateScript);
            EditScriptCommand = new RelayCommand<GameInfo>(ExecuteEditScript, CanEditScript);
            DeleteScriptCommand = new RelayCommand<GameInfo>(ExecuteDeleteScript, CanDeleteScript);
            ToggleScriptEnabledByNameCommand = new RelayCommand<string>(param => ExecuteToggleScriptByName(param));
            EditScriptByNameCommand = new RelayCommand<string>(param => ExecuteEditScriptByName(param));
            DeleteScriptByNameCommand = new RelayCommand<string>(param => ExecuteDeleteScriptByName(param));
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
            // confirm deletion
            if (System.Windows.MessageBox.Show($"Delete '{game.Name}'?", "Confirm Delete",
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            Games.Remove(game);
            SaveGames();
            GamesView.Refresh();
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
        }

        private void ApplyGrouping()
        {
            var consoles = GamesView
                .Cast<GameInfo>()
                .Select(g => g.ConsoleName ?? "(null)")
                .Distinct();

            Debug.WriteLine("Unique consoles: " + string.Join(", ", consoles));

            GamesView.GroupDescriptions.Clear();
            GamesView.SortDescriptions.Clear();

            // set group description based on selected criteria
            PropertyGroupDescription? pgd = GroupBy switch
            {
                "Console" => new PropertyGroupDescription(nameof(GameInfo.ConsoleName)),
                "Genre" => new PropertyGroupDescription(nameof(GameInfo.Genre)),
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
            return Directory.Exists(dataDir) && Directory.GetFiles(dataDir, "*", SearchOption.AllDirectories).Any();
        }

        private bool CanActivateGame(GameInfo? game)
        {
            if (game == null) return false;
            if (!game.IsInArchive) return false;
            var s = _settings.Load();
            if (string.IsNullOrEmpty(game.GameGuid)) return false;
            var basePath = Path.Combine(s.ArchiveLibraryPath, "Game Data");
            var zip = Path.Combine(basePath, game.GameGuid + ".zip");
            var folder = Path.Combine(basePath, game.GameGuid);
            return File.Exists(zip) || Directory.Exists(folder);
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
