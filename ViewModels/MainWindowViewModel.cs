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

namespace SamsGameLauncher.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        // ──── Paths ─────────────────────────────────────────────────────────
        private readonly string _basePath = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string _dataFolder;
        private readonly string _gamesFile;
        private readonly string _emuFile;

        private readonly IDialogService _dialogs;    // injected dialog service
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
                ((RelayCommand)RunCommand).RaiseCanExecuteChanged();
                ((RelayCommand)EditGameCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteGameCommand).RaiseCanExecuteChanged();
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
        public ICommand AddEmuCommand { get; }

        // ──── Constructor ──────────────────────────────────────────────────
        public MainWindowViewModel(IDialogService dialogs)
        {
            _dialogs = dialogs;
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

            // create view for grouping & filtering
            GamesView = CollectionViewSource.GetDefaultView(Games);
            ApplyGrouping();
            ApplyFilter();

            // wire up commands
            RunCommand = new RelayCommand(
                o =>
                {
                    if (o is GameBase game) RunGame(game);
                },
                o => o is GameBase
            );
            AddGameCommand = new RelayCommand(_ => ExecuteAddGame());
            EditGameCommand = new RelayCommand(
                o =>
                {
                    if (o is GameBase game) ExecuteEditGame(game);
                },
                o => o is GameBase
            );
            DeleteGameCommand = new RelayCommand(
                o =>
                {
                    if (o is GameBase game) ExecuteDeleteGame(game);
                },
                o => o is GameBase
            );
            AddEmuCommand = new RelayCommand(_ => ExecuteAddEmulator());
        }

        // ──── Actions ───────────────────────────────────────────────────────
        private void RunGame(GameBase game)
        {
            try
            {
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

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = em.Emulator.ExecutablePath,
                            Arguments = emArgs,
                            UseShellExecute = true
                        });
                        break;

                    case NativeGame ng:
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = ng.ExePath,
                            UseShellExecute = true
                        });
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

                        Process.Start(new ProcessStartInfo
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
    }
}