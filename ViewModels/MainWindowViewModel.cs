using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using SamsGameLauncher.Models;
using SamsGameLauncher.Commands;
using SamsGameLauncher.Services;
using System.Numerics;
using System.Windows.Forms;
using SamsGameLauncher.Configuration;
using MessageBox = System.Windows.MessageBox;
using CommunityToolkit.Mvvm.Input;
using SamsGameLauncher.Views;
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
                ((SamsGameLauncher.Commands.RelayCommand)RunCommand).RaiseCanExecuteChanged();
                ((SamsGameLauncher.Commands.RelayCommand)EditGameCommand).RaiseCanExecuteChanged();
                ((SamsGameLauncher.Commands.RelayCommand)DeleteGameCommand).RaiseCanExecuteChanged();
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
        public ICommand RunCommand { get; }
        public ICommand AddGameCommand { get; }
        public ICommand EditGameCommand { get; }
        public ICommand DeleteGameCommand { get; }
        public ICommand AddEmulatorCommand { get; }
        public ICommand ShowSettingsCommand { get; }
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
            RunCommand = new SamsGameLauncher.Commands.RelayCommand(
                o =>
                {
                    if (o is GameBase game) RunGame(game);
                },
                o => o is GameBase
            );
            AddGameCommand = new SamsGameLauncher.Commands.RelayCommand(_ => ExecuteAddGame());
            EditGameCommand = new SamsGameLauncher.Commands.RelayCommand(
                o =>
                {
                    if (o is GameBase game) ExecuteEditGame(game);
                },
                o => o is GameBase
            );
            DeleteGameCommand = new SamsGameLauncher.Commands.RelayCommand(
                o =>
                {
                    if (o is GameBase game) ExecuteDeleteGame(game);
                },
                o => o is GameBase
            );
            AddEmulatorCommand = new SamsGameLauncher.Commands.RelayCommand(_ => ExecuteAddEmulator());
            ShowSettingsCommand = new SamsGameLauncher.Commands.RelayCommand(p =>
            {
                if (p is string section)
                    dialogs.ShowSettings(section);
            });
            ArchiveGameCommand = new AsyncRelayCommand<GameBase>(
                async g => await MoveGameAsync(g, toArchive: true),
                g => g != null);
            ActivateGameCommand = new AsyncRelayCommand<GameBase>(
                async g => await MoveGameAsync(g, toArchive: false),
                g => g != null);
            _fileMover = fileMover;
        }

        // ──── Actions ───────────────────────────────────────────────────────
        private void RunGame(GameBase game)
        {
            try
            {
                Process? process = null;

                // handle each game type
                switch (game)
                {
                    case EmulatedGame em:
                        if (em.Emulator == null)
                        {
                            System.Windows.MessageBox.Show("No emulator associated.", "Launch Error",
                                            MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        // substitute {RomPath} placeholder if needed
                        var emArgs = string.IsNullOrWhiteSpace(em.Emulator.DefaultArguments)
                                     ? $"\"{em.GamePath}\""
                                     : em.Emulator.DefaultArguments.Replace("{RomPath}", em.GamePath);

                        process = Process.Start(new ProcessStartInfo
                        {
                            FileName = em.Emulator.ExecutablePath,
                            Arguments = emArgs,
                            UseShellExecute = true
                        });
                        break;

                    case NativeGame ng:
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = ng.ExePath,
                            WorkingDirectory = Path.GetDirectoryName(ng.ExePath)!,
                            UseShellExecute = false          // honour WorkingDirectory, allow args > 2 kB
                        };
                        process = Process.Start(startInfo);
                        break;

                    case FolderBasedGame fg:
                        if (fg.Emulator == null)
                        {
                            System.Windows.MessageBox.Show("No emulator associated.", "Launch Error",
                                            MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        var fgArgs = string.IsNullOrWhiteSpace(fg.Emulator.DefaultArguments)
                                     ? $"\"{fg.FolderPath}\""
                                     : fg.Emulator.DefaultArguments.Replace("{FolderPath}", fg.FolderPath);

                        process = Process.Start(new ProcessStartInfo
                        {
                            FileName = fg.Emulator.ExecutablePath,
                            Arguments = fgArgs,
                            UseShellExecute = true
                        });
                        break;

                    default:
                        System.Windows.MessageBox.Show("Unsupported game type.", "Launch Error",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                }

                if (process != null)
                {
                    // 1) Load the user’s saved monitor index
                    var model = _settings.Load();
                    var screens = Screen.AllScreens;
                    int idx = model.DefaultMonitorIndex;

                    Debug.WriteLine($"[RunGame] Saved idx={idx}, screen count={screens.Length}");
                    // If you still want to log each monitor’s bounds, do this instead:
                    foreach (var s in screens)
                        Debug.WriteLine($"{s.DeviceName}: {s.Bounds.Width}×{s.Bounds.Height} @ ({s.Bounds.X},{s.Bounds.Y})");

                    // 2) pick the target (or fallback)
                    var target = (idx >= 0 && idx < screens.Length)
                                   ? screens[idx]
                                   : Screen.PrimaryScreen;
                    var fallback = Screen.PrimaryScreen;

                    // 3) move & resize the emulator windows
                    _placer.PlaceProcessWindows(process, target, fallback);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to launch game:\n{ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteAddGame()
        {
            // show dialog and get new game
            var newGame = _dialogs.ShowAddGame(Emulators);
            if (newGame == null) return;

            // wire up emulator object
            if (newGame is EmulatedGame eem)
                eem.Emulator = Emulators.FirstOrDefault(e => e.Id == eem.EmulatorId);
            else if (newGame is FolderBasedGame fgm)
                fgm.Emulator = Emulators.FirstOrDefault(e => e.Id == fgm.EmulatorId);

            Games.Add(newGame);
            SaveGames();
        }

        private void ExecuteEditGame(GameBase game)
        {
            // show edit dialog; returns null if cancelled
            var edited = _dialogs.ShowEditGame(game, Emulators);
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
                "Console" => new PropertyGroupDescription(nameof(GameBase.Console)),
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

        private async void MoveGame(GameBase? game, bool toArchive)
        {
            if (game == null) return;

            var settings = _settings.Load();
            var srcRoot = toArchive ? settings.ActiveLibraryPath : settings.ArchiveLibraryPath;
            var destRoot = toArchive ? settings.ArchiveLibraryPath : settings.ActiveLibraryPath;

            // 1) real install folder
            var srcFolder = GetInstallRoot(game, settings.ActiveLibraryPath, settings.ArchiveLibraryPath);
            if (string.IsNullOrWhiteSpace(srcFolder)) return;

            // safety check
            if (!srcFolder.StartsWith(srcRoot, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Game is already on the requested drive.", "Move",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 2) target: <destRoot>\<Console>\<GameName>
            var console = game.Console;
            var newFolder = Path.Combine(destRoot, console, game.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(newFolder)!);

            try
            {
                if (Path.GetPathRoot(srcFolder).Equals(Path.GetPathRoot(newFolder),
                                                       StringComparison.OrdinalIgnoreCase))
                {
                    Directory.Move(srcFolder, newFolder);        // same drive
                }
                else
                {
                    Directory.CreateDirectory(newFolder);
                    await Task.Run(() => CopyDirectory(srcFolder, newFolder));
                    Directory.Delete(srcFolder, true);
                }

                // 3) rebuild each path *relative* to the new folder
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
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to move game:\n{ex.Message}",
                                "Move Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // helper to copy an entire directory tree
        private void CopyDirectory(string src, string dst)
        {
            // create all sub‑folders
            foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dir.Replace(src, dst));
            }
            // copy files
            foreach (var file in Directory.GetFiles(src, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(src, dst), overwrite: true);
            }
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
                if (string.Equals(Path.GetFileName(parent), game.Console,
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

            var newFolder = Path.Combine(destRoot, game.Console, game.Name);
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