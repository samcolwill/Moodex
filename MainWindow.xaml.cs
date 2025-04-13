using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;
using System.Collections.ObjectModel;
using SamsGameLauncher.Models;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;
using System.ComponentModel;

namespace SamsGameLauncher
{
    public partial class MainWindow : Window
    {
        // Store the base path and Data folder as read-only properties.
        private readonly string _basePath = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string _dataFolder;

        // Ensure Games is an ObservableCollection (update GameLibrary accordingly).
        private GameLibrary _gameLibrary = new GameLibrary();

        public MainWindow()
        {
            InitializeComponent();

            _dataFolder = System.IO.Path.Combine(_basePath, "Data");

            try
            {
                // Initialize and load data from the Data folder.
                _gameLibrary.InitializeAndLoadData(_dataFolder);
                // Set the DataContext to _gameLibrary so that XAML bindings work.
                this.DataContext = _gameLibrary;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load game library: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper method that encapsulates the game launching logic.
        private void RunGame(Game game)
        {
            try
            {
                if (game.Emulator == null)
                {
                    MessageBox.Show("No emulator is associated with this game.", "Launch Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string emulatorPath = game.Emulator.ExecutablePath;
                string gamePath = game.GamePath;

                // For RPCS3: if a file called PARAM.SFO is selected, launch using its parent folder.
                if (game.Emulator.Id.Equals("rpcs3", StringComparison.OrdinalIgnoreCase)
                    && Path.GetFileName(game.GamePath).Equals("PARAM.SFO", StringComparison.OrdinalIgnoreCase))
                {
                    gamePath = Path.GetDirectoryName(game.GamePath);
                }

                // Determine arguments: if there are no default arguments, wrap the gamePath in quotes.
                string arguments = string.IsNullOrWhiteSpace(game.Emulator.DefaultArguments)
                    ? $"\"{gamePath}\""
                    : game.Emulator.DefaultArguments.Replace("{RomPath}", gamePath);

                Process.Start(new ProcessStartInfo
                {
                    FileName = emulatorPath,
                    Arguments = arguments,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch game:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Click handler for game tiles (left-click).
        private void GameCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Game game)
            {
                RunGame(game);
            }
        }

        // "Run" from the context menu.
        private void RunMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is Game game)
            {
                RunGame(game);
            }
        }

        // "Edit" context menu click.
        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is Game game)
            {
                // Open the edit window (EditGameWindow) and pass the game to edit along with available emulators.
                EditGameWindow editWindow = new EditGameWindow(game, _gameLibrary.Emulators);
                if (editWindow.ShowDialog() == true)
                {
                    // The game is updated in place. Save the updated collection.
                    SaveGames();
                }
            }
            RefreshGroups();
        }

        // "Delete" context menu click.
        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is Game game)
            {
                if (MessageBox.Show($"Delete {game.Name}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.Yes)
                {
                    _gameLibrary.Games.Remove(game);
                    SaveGames();
                }
            }
            RefreshGroups();
        }

        // "Add Game" menu command click handler.
        private void AddGame_Click(object sender, RoutedEventArgs e)
        {
            AddGameWindow addGameWindow = new AddGameWindow(_gameLibrary.Emulators);
            if (addGameWindow.ShowDialog() == true)
            {
                Game newGame = addGameWindow.NewGame;
                if (newGame != null)
                {
                    // Associate the appropriate emulator.
                    newGame.Emulator = _gameLibrary.Emulators.FirstOrDefault(em => em.Id == newGame.EmulatorId);
                    // Add the new game (since Games is an ObservableCollection, UI updates automatically).
                    _gameLibrary.Games.Add(newGame);
                    SaveGames();
                }
            }
        }

        // "Add Emulator" menu command click handler.
        private void AddEmulator_Click(object sender, RoutedEventArgs e)
        {
            AddEmulatorWindow addEmulatorWindow = new AddEmulatorWindow();
            if (addEmulatorWindow.ShowDialog() == true)
            {
                Emulator newEmulator = addEmulatorWindow.NewEmulator;
                if (newEmulator != null)
                {
                    _gameLibrary.Emulators.Add(newEmulator);
                    SaveEmulators();
                }
            }
        }

        // Save games to disk.
        private void SaveGames()
        {
            string gamePath = Path.Combine(_basePath, "Data", "games.json");
            _gameLibrary.SaveGames(gamePath);
        }

        // Save emulators to disk.
        private void SaveEmulators()
        {
            string emulatorPath = Path.Combine(_basePath, "Data", "emulators.json");
            _gameLibrary.SaveEmulators(emulatorPath);
        }

        // Refresh the CollectionViewSource view (for grouping/sorting).
        private void RefreshGroups()
        {
            var cvs = this.FindResource("GroupedGames") as CollectionViewSource;
            cvs?.View.Refresh();
        }

        // Search TextBox event handlers.
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Search...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Search...";
                SearchTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b2b2b2"));
            }
        }

        // Filtering functionality for the search bar.
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var cvs = this.FindResource("GroupedGames") as CollectionViewSource;
            if (cvs?.View == null)
                return;

            string searchText = SearchTextBox.Text;
            cvs.View.Filter = o =>
            {
                if (o is Game game)
                {
                    if (string.IsNullOrWhiteSpace(searchText) || searchText.Equals("Search...", StringComparison.OrdinalIgnoreCase))
                        return true;
                    return game.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                }
                return true;
            };

            cvs.View.Refresh();
        }

        // Group By ComboBox event handler.
        private void GroupByComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupByComboBox == null)
                return;

            var selectedItem = GroupByComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null || selectedItem.Content == null)
                return;

            string groupBy = selectedItem.Content.ToString();
            var cvs = this.FindResource("GroupedGames") as CollectionViewSource;
            if (cvs?.View == null)
                return;

            cvs.GroupDescriptions.Clear();
            cvs.SortDescriptions.Clear();

            if (groupBy == "Console")
            {
                cvs.GroupDescriptions.Add(new PropertyGroupDescription("Console"));
                cvs.SortDescriptions.Add(new SortDescription("Console", ListSortDirection.Ascending));
                cvs.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            }
            else if (groupBy == "Genre")
            {
                cvs.GroupDescriptions.Add(new PropertyGroupDescription("Genre"));
                cvs.SortDescriptions.Add(new SortDescription("Genre", ListSortDirection.Ascending));
                cvs.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            }
            else if (groupBy == "Year")
            {
                cvs.GroupDescriptions.Add(new PropertyGroupDescription("ReleaseYear"));
                cvs.SortDescriptions.Add(new SortDescription("ReleaseYear", ListSortDirection.Ascending));
                cvs.SortDescriptions.Add(new SortDescription("ReleaseDate", ListSortDirection.Ascending));
            }

            cvs.View.Refresh();
        }
    }
}