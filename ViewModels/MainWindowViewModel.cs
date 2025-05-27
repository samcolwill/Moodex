using CommunityToolkit.Mvvm.Input;
using SamsGameLauncher.Converters;
using SamsGameLauncher.Models;
using SamsGameLauncher.Services;
using SamsGameLauncher.Views.Utilities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace SamsGameLauncher.ViewModels
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

        // ──── Constructor ──────────────────────────────────────────────────
        public MainWindowViewModel(IDialogService dialogs, IWindowPlacementService placer, ISettingsService settings, IFileMoveService fileMover)
        {
            _dialogs = dialogs;
            _placer = placer;
            _settings = settings;
            _fileMover = fileMover;
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
                }
            });
            ShowAboutCommand = new RelayCommand(ExecuteShowAbout);
            ArchiveGameCommand = new AsyncRelayCommand<GameInfo>(g => MoveGameAsync(g, toArchive: true), g => g is not null);
            ActivateGameCommand = new AsyncRelayCommand<GameInfo>(g => MoveGameAsync(g, toArchive: false), g => g is not null);

            _fileMover = fileMover;
        }

        // ──── Actions ───────────────────────────────────────────────────────
        private void RunGame(GameInfo game)
        {
            try
            {
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
                    // Position its window on the user’s chosen monitor
                    var settings = _settings.Load();
                    var screens = Screen.AllScreens;
                    int idx = settings.DefaultMonitorIndex;
                    var targetScreen = (idx >= 0 && idx < screens.Length)
                                       ? screens[idx]
                                       : Screen.PrimaryScreen;

                    _placer.PlaceProcessWindows(process, targetScreen, Screen.PrimaryScreen);
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

        private void ExecuteEditGame(GameInfo game)
        {
            // show edit dialog; returns null if cancelled
            var edited = _dialogs.ShowEditGame(game);
            if (edited == null) return;

            SaveGames();
            GamesView.Refresh();
        }

        private void ExecuteDeleteGame(GameInfo game)
        {
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
                    return string.IsNullOrWhiteSpace(SearchText)
                        || g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
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

            // avoid trying to move if already on target drive
            if (!srcFolder.StartsWith(srcRoot, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Game is already on the requested drive.", "Move",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // create new <Drive>\<Console>\<GameName> folder
            var consoleName = game.ConsoleName;
            var newFolder = Path.Combine(dstRoot, consoleName, game.Name);
            Directory.CreateDirectory(newFolder);

            // show progress…
            var vm = new ProgressWindowViewModel();
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
                success = await _fileMover.MoveFolderAsync(srcFolder, newFolder, prog, vm.Token);
            }
            catch (OperationCanceledException) { /* user hit Cancel */ }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to move game:\n{ex.Message}",
                                "Move Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { dlg.Close(); }

            if (!success) return;   // aborted or failed

            // rebuild the stored path for EVERY game the same way:
            //   - if we moved a file, preserve its filename under newFolder
            //   - if we moved a folder, that folder IS newFolder
            var rel = Path.GetRelativePath(srcFolder, game.FileSystemPath);
            game.FileSystemPath = (rel == "." || string.IsNullOrEmpty(rel))
                ? newFolder
                : Path.Combine(newFolder, rel);

            game.IsInArchive = toArchive;
            SaveGames();
            GamesView.Refresh();
        }

    }
}