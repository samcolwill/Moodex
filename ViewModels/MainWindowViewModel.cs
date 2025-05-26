using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using SamsGameLauncher.Models;
using SamsGameLauncher.Services;
using MessageBox = System.Windows.MessageBox;
using CommunityToolkit.Mvvm.Input;
using SamsGameLauncher.Views.Utilities;
using Application = System.Windows.Application;

namespace SamsGameLauncher.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        // ──── Paths ─────────────────────────────────────────────────────────
        private readonly string _basePath = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string _dataFolder;
        private readonly string _gamesFile;
        private readonly string _emuFile;

        private readonly IDialogService _dialogs;
        private readonly IWindowPlacementService _placer;
        private readonly ISettingsService _settings;
        private readonly IFileMoveService _fileMover;

        private readonly GameLibrary _gameLibrary;

        // ──── Exposed Collections & Views ──────────────────────────────────
        public ObservableCollection<GameBase> Games => _gameLibrary.Games;
        public List<Emulator> Emulators => _gameLibrary.Emulators;
        public ICollectionView GamesView { get; }

        // ──── Selection ────────────────────────────────────────────────────
        private GameBase? _selectedGame;
        public GameBase? SelectedGame
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
                var p = g switch
                {
                    EmulatedGame em => em.GamePath,
                    NativeGame ng => ng.ExePath,
                    FolderBasedGame fg => fg.FolderPath,
                    _ => ""
                };

                g.IsInArchive = p.StartsWith(s.ArchiveLibraryPath,
                                             StringComparison.OrdinalIgnoreCase);
            }

            // create view for grouping & filtering
            GamesView = CollectionViewSource.GetDefaultView(Games);
            ApplyGrouping();
            ApplyFilter();

            // wire up commands
            RunCommand = new RelayCommand<GameBase>(RunGame, game => game is not null);
            AddGameCommand = new RelayCommand(ExecuteAddGame);
            EditGameCommand = new RelayCommand<GameBase>(ExecuteEditGame, game => game is not null);
            DeleteGameCommand = new RelayCommand<GameBase>(ExecuteDeleteGame, game => game is not null);
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
            ArchiveGameCommand = new AsyncRelayCommand<GameBase>(g => MoveGameAsync(g, toArchive: true), g => g is not null);
            ActivateGameCommand = new AsyncRelayCommand<GameBase>(g => MoveGameAsync(g, toArchive: false), g => g is not null);

            _fileMover = fileMover;
        }

        // ──── Actions ───────────────────────────────────────────────────────
        private void RunGame(GameBase game)
        {
            try
            {
                Process? process = null;

                switch (game)
                {
                    case EmulatedGame em:
                        {
                            // Find the emulator for this console
                            var emulator = Emulators.FirstOrDefault(e => e.ConsoleEmulated == em.Console);
                            if (emulator == null)
                            {
                                MessageBox.Show($"No emulator configured for {em.Console.GetDescription()}.",
                                    "Launch Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            var emArgs = string.IsNullOrWhiteSpace(emulator.DefaultArguments)
                                ? $"\"{em.GamePath}\""
                                : emulator.DefaultArguments.Replace("{RomPath}", em.GamePath);

                            process = Process.Start(new ProcessStartInfo
                            {
                                FileName = emulator.ExecutablePath,
                                Arguments = emArgs,
                                UseShellExecute = true
                            });
                            break;
                        }

                    case FolderBasedGame fg:
                        {
                            var emulator = Emulators.FirstOrDefault(e => e.ConsoleEmulated == fg.Console);
                            if (emulator == null)
                            {
                                MessageBox.Show($"No emulator configured for {fg.Console.GetDescription()}.",
                                    "Launch Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            var fgArgs = string.IsNullOrWhiteSpace(emulator.DefaultArguments)
                                ? $"\"{fg.FolderPath}\""
                                : emulator.DefaultArguments.Replace("{FolderPath}", fg.FolderPath);

                            process = Process.Start(new ProcessStartInfo
                            {
                                FileName = emulator.ExecutablePath,
                                Arguments = fgArgs,
                                UseShellExecute = true
                            });
                            break;
                        }

                    case NativeGame ng:
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = ng.ExePath,
                                WorkingDirectory = Path.GetDirectoryName(ng.ExePath)!,
                                UseShellExecute = false
                            };
                            process = Process.Start(startInfo);
                            break;
                        }

                    default:
                        MessageBox.Show("Unsupported game type.", "Launch Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                }

                // Existing logic for monitor selection and placement...
                if (process != null)
                {
                    // ... (unchanged)
                    var model = _settings.Load();
                    var screens = Screen.AllScreens;
                    int idx = model.DefaultMonitorIndex;

                    Debug.WriteLine($"[RunGame] Saved idx={idx}, screen count={screens.Length}");
                    foreach (var s in screens)
                        Debug.WriteLine($"{s.DeviceName}: {s.Bounds.Width}×{s.Bounds.Height} @ ({s.Bounds.X},{s.Bounds.Y})");

                    var target = (idx >= 0 && idx < screens.Length) ? screens[idx] : Screen.PrimaryScreen;
                    var fallback = Screen.PrimaryScreen;

                    _placer.PlaceProcessWindows(process, target, fallback);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch game:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void ExecuteEditGame(GameBase game)
        {
            // show edit dialog; returns null if cancelled
            var edited = _dialogs.ShowEditGame(game);
            if (edited == null) return;

            SaveGames();
            GamesView.Refresh();
        }

        private void ExecuteDeleteGame(GameBase game)
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
                if (o is GameBase g)
                    return string.IsNullOrWhiteSpace(SearchText)
                        || g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
                return true;
            };
            GamesView.Refresh();
        }

        private void ApplyGrouping()
        {
            GamesView.GroupDescriptions.Clear();
            GamesView.SortDescriptions.Clear();

            // set group description based on selected criteria
            PropertyGroupDescription? pgd = GroupBy switch
            {
                "Console" => new PropertyGroupDescription(nameof(GameBase.ConsoleName)),
                "Genre" => new PropertyGroupDescription(nameof(GameBase.Genre)),
                "Year" => new PropertyGroupDescription(nameof(GameBase.ReleaseYear)),
                _ => null
            };

            if (pgd != null)
            {
                GamesView.GroupDescriptions.Add(pgd);
                GamesView.SortDescriptions.Add(new SortDescription(pgd.PropertyName, ListSortDirection.Ascending));
                GamesView.SortDescriptions.Add(new SortDescription(nameof(GameBase.Name), ListSortDirection.Ascending));
            }

            GamesView.Refresh();
        }

        public static string GetInstallRoot(GameBase game,
                                            string activeRoot,
                                            string archiveRoot)
        {
            // 0) pick the path we actually know
            string path = game switch
            {
                EmulatedGame em => em.GamePath,
                NativeGame ng => ng.ExePath,
                FolderBasedGame fg => fg.FolderPath,
                _ => ""
            };
            if (string.IsNullOrEmpty(path)) return "";

            // 1) start from the folder that *contains* the file (if any)
            string dir = File.Exists(path) ? Path.GetDirectoryName(path)! : path;

            // 2) walk up until the parent is the console folder (PC, PS2, etc.)
            while (true)
            {
                string? parent = Path.GetDirectoryName(dir);
                if (parent == null) break;

                // If the parent is the console folder, we've reached <GameName>
                var consoleName = game.Console.GetDescription();
                if (string.Equals(Path.GetFileName(parent), consoleName,
                                  StringComparison.OrdinalIgnoreCase))
                    return dir;                      // <- stop here

                // If we're about to escape the library roots, bail out
                if (parent.Equals(activeRoot, StringComparison.OrdinalIgnoreCase) ||
                    parent.Equals(archiveRoot, StringComparison.OrdinalIgnoreCase))
                    return dir;                      // unusual layout but safe

                dir = parent;                        // climb one level
            }
            return dir;      // fallback (shouldn't really happen)
        }

        private async Task MoveGameAsync(GameBase? game, bool toArchive)
        {
            if (game == null) return;

            var set = _settings.Load();
            var srcRoot = toArchive ? set.ActiveLibraryPath : set.ArchiveLibraryPath;
            var destRoot = toArchive ? set.ArchiveLibraryPath : set.ActiveLibraryPath;

            // 1) resolve folders exactly as before
            var srcFolder = GetInstallRoot(game, set.ActiveLibraryPath, set.ArchiveLibraryPath);

            if (string.IsNullOrWhiteSpace(srcFolder)) return;

            if (!srcFolder.StartsWith(srcRoot, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Game is already on the requested drive.", "Move",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var consoleName = game.Console.GetDescription();
            var newFolder = Path.Combine(destRoot, consoleName, game.Name);
            Directory.CreateDirectory(newFolder);

            // 2) show progress dialog
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
                MessageBox.Show($"Failed to move game:\n{ex.Message}", "Move Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { dlg.Close(); }

            if (!success) return;   // aborted or error

            // 3) rebuild stored paths (same logic you had before)
            switch (game)
            {
                case EmulatedGame em:
                    em.GamePath = Path.Combine(newFolder,
                                  Path.GetRelativePath(srcFolder, em.GamePath));
                    break;
                case NativeGame ng:
                    ng.ExePath = Path.Combine(newFolder,
                                  Path.GetRelativePath(srcFolder, ng.ExePath));
                    break;
                case FolderBasedGame fg:
                    fg.FolderPath = newFolder;
                    break;
            }

            game.IsInArchive = toArchive;
            SaveGames();
            GamesView.Refresh();
        }
    }
}