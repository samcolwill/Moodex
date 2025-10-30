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
        private readonly GameLibrary _gameLibrary;

        // ──── Services ───────────────────────────────────────────────────────
        private readonly IDialogService _dialogs;
        private readonly IWindowPlacementService _placer;
        private readonly ISettingsService _settings;
        private readonly IFileMoveService _fileMover;
        private readonly IAutoHotKeyScriptService _scriptService;
        private readonly Dictionary<int, Process> _trackedGameProcesses = new();

        // ──── Exposed Collections & Views ──────────────────────────────────
        public ObservableCollection<GameInfo> Games => _gameLibrary.Games;
        public List<EmulatorInfo> Emulators => _gameLibrary.Emulators;
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

        // ──── Constructor ──────────────────────────────────────────────────
        public MainWindowViewModel(IDialogService dialogs, IWindowPlacementService placer, ISettingsService settings, IFileMoveService fileMover, IAutoHotKeyScriptService scriptService)
        {
            _dialogs = dialogs;
            _placer = placer;
            _settings = settings;
            _fileMover = fileMover;
            _scriptService = scriptService;
            _dataFolder = Path.Combine(_basePath, "Data");
            _gamesFile = Path.Combine(_dataFolder, "games.json");
            _emuFile = Path.Combine(_dataFolder, "emulators.json");

            // load model
            _gameLibrary = new GameLibrary();
            try
            {
                _gameLibrary.InitializeAndLoadData(_dataFolder);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load game library: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }

            var s = _settings.Load();
            foreach (var g in Games)
            {
                var p = g.FileSystemPath;
                g.IsInArchive = p.StartsWith(s.ArchiveLibraryPath,
                                             StringComparison.OrdinalIgnoreCase);
            }

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
            ArchiveGameCommand = new AsyncRelayCommand<GameInfo>(g => MoveGameAsync(g, toArchive: true), g => g is not null);
            ActivateGameCommand = new AsyncRelayCommand<GameInfo>(g => MoveGameAsync(g, toArchive: false), g => g is not null);

            // AutoHotKey Script Commands
            CreateScriptCommand = new RelayCommand<GameInfo>(ExecuteCreateScript, CanCreateScript);
            EditScriptCommand = new RelayCommand<GameInfo>(ExecuteEditScript, CanEditScript);
            DeleteScriptCommand = new RelayCommand<GameInfo>(ExecuteDeleteScript, CanDeleteScript);

            _fileMover = fileMover;
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
                    if (IsConsoleAhkEnabled(game.ConsoleId) && _scriptService.HasScript(game))
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

        // ──── Persistence ───────────────────────────────────────────────────
        private void SaveGames() => _gameLibrary.SaveGames(_gamesFile);
        private void SaveEmulators() => _gameLibrary.SaveEmulators(_emuFile);

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
            var srcRoot = toArchive ? set.ActiveLibraryPath : set.ArchiveLibraryPath;
            var dstRoot = toArchive ? set.ArchiveLibraryPath : set.ActiveLibraryPath;

            // resolve the folder that actually contains the game
            var srcFolder = GetInstallRoot(game, set.ActiveLibraryPath, set.ArchiveLibraryPath);
            if (string.IsNullOrWhiteSpace(srcFolder)) return;

            // Check if we're extracting a zip file (moving from archive)
            bool isSourceZipped = !toArchive && srcFolder.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
            
            // avoid trying to move if already on target drive
            if (!isSourceZipped && !srcFolder.StartsWith(srcRoot, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Game is already on the requested drive.", "Move",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // create new <Drive>\<Console>\<GameName> folder
            var consoleName = game.ConsoleName;
            if (string.IsNullOrEmpty(consoleName))
            {
                MessageBox.Show("Game console name is not set.", "Move Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var newFolder = Path.Combine(dstRoot, consoleName, game.Name);
            
            // When extracting a zip, extract to the console directory (zip contains the game folder)
            // Otherwise, create the game folder
            if (isSourceZipped)
            {
                // Extract to console directory, the zip will create the game folder
                newFolder = Path.Combine(dstRoot, consoleName);
                Directory.CreateDirectory(newFolder);
            }
            else if (!(toArchive && set.CompressOnArchive))
            {
                // Only create directory if not compressing (compression will handle it differently)
                Directory.CreateDirectory(newFolder);
            }

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
            var prog = new Progress<MoveProgress>(p =>
            {
                vm.Percent = p.Percent;
                vm.CurrentFile = p.CurrentFile;
            });

            bool success = false;
            try
            {
                // Pass compression settings to the file mover
                bool shouldCompress = toArchive && set.CompressOnArchive && !string.IsNullOrEmpty(set.SevenZipPath);
                success = await _fileMover.MoveFolderAsync(srcFolder, newFolder, prog, vm.Token, shouldCompress, set.SevenZipPath);
            }
            catch (OperationCanceledException) { /* user hit Cancel */ }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to move game:\n{ex.Message}",
                                "Move Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { dlg.Close(); }

            if (!success) return;   // aborted or failed

            // Update the stored path based on whether we compressed or extracted
            if (toArchive && set.CompressOnArchive)
            {
                // Game was compressed - point to the archive folder (not the zip)
                // This folder contains both the cover image and the zip file
                // Structure: <Archive>\<Console>\<GameName>\ with <GameName>.jpg and <GameName>.zip inside
                var archiveFolder = Path.Combine(dstRoot, consoleName, game.Name);
                game.FileSystemPath = archiveFolder;
            }
            else if (isSourceZipped)
            {
                // Game was extracted from a zip file
                // The zip contains a folder with the game name, which was extracted to the console directory
                // So the extracted game folder is at: <dstRoot>\<Console>\<GameName>\
                var extractedGameFolder = Path.Combine(dstRoot, consoleName, game.Name);
                game.FileSystemPath = extractedGameFolder;
                
                // Clean up the archive folder (which contained the cover and zip)
                try
                {
                    var archiveFolder = Path.GetDirectoryName(srcFolder);
                    if (!string.IsNullOrEmpty(archiveFolder) && Directory.Exists(archiveFolder))
                    {
                        Directory.Delete(archiveFolder, recursive: true);
                    }
                }
                catch (Exception ex)
                {
                    // Don't fail if cleanup fails
                    System.Diagnostics.Debug.WriteLine($"Failed to clean up archive folder: {ex.Message}");
                }
            }
            else
            {
                // Normal folder move
                var finalFolder = Path.Combine(dstRoot, consoleName, game.Name);
                var rel = Path.GetRelativePath(srcFolder, game.FileSystemPath);
                game.FileSystemPath = (rel == "." || string.IsNullOrEmpty(rel))
                    ? finalFolder
                    : Path.Combine(finalFolder, rel);
            }

            game.IsInArchive = toArchive;
            SaveGames();
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
                _scriptService.EditScript(game);
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
            return game != null &&
                   IsConsoleAhkEnabled(game.ConsoleId) &&
                   IsAutoHotKeyInstalled();
        }

        private bool CanEditScript(GameInfo? game)
        {
            return game != null &&
                   IsConsoleAhkEnabled(game.ConsoleId) &&
                   _scriptService.HasScript(game) &&
                   IsAutoHotKeyInstalled();
        }

        private bool CanDeleteScript(GameInfo? game)
        {
            return game != null &&
                   IsConsoleAhkEnabled(game.ConsoleId) &&
                   _scriptService.HasScript(game) &&
                   IsAutoHotKeyInstalled();
        }

        private bool IsAutoHotKeyInstalled()
        {
            var settings = _settings.Load();
            return settings.IsAutoHotKeyInstalled;
        }

        private bool IsConsoleAhkEnabled(string consoleId)
        {
            try
            {
                var settings = _settings.Load();
                return settings.AhkEnabledConsoleIds?.Contains(consoleId, StringComparer.OrdinalIgnoreCase) == true;
            }
            catch
            {
                return false;
            }
        }

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
                if (IsConsoleAhkEnabled(game.ConsoleId) && _scriptService.HasScript(game))
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
                
                // Check if the command line contains the game's script path
                var scriptPath = _scriptService.GetAhkScriptPath(game);
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    return commandLine.Contains(scriptPath, StringComparison.OrdinalIgnoreCase);
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
