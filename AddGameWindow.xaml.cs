using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using SamsGameLauncher.Models;
using SamsGameLauncher.Constants;

namespace SamsGameLauncher
{
    public partial class AddGameWindow : Window
    {
        // NewGame is now of type GameBase
        public GameBase NewGame { get; private set; }

        public List<string> Consoles { get; set; }
        public List<string> Genres { get; set; }
        public List<Emulator> Emulators { get; set; }
        // A list of available game types (string representations of GameType enum).
        public List<string> GameTypes { get; set; }

        public AddGameWindow(List<Emulator> availableEmulators)
        {
            InitializeComponent();

            // Get predefined consoles and genres from constants.
            Consoles = LauncherConstants.Consoles;
            Genres = LauncherConstants.Genres;
            Emulators = availableEmulators;
            // Initialize available game types (these should match your enum names).
            GameTypes = Enum.GetNames(typeof(GameType)).ToList();

            DataContext = this;

            // Disable controls until a game type is selected.
            GamePathTextBox.IsEnabled = false;
            EmulatorComboBox.IsEnabled = false;
            ConsoleComboBox.IsEnabled = false;
            GenreComboBox.IsEnabled = false;
            ReleaseDatePicker.IsEnabled = false;
            BrowseGameFileFolderButton.IsEnabled = false;
        }

        // Browse for Game File.
        private void BrowseGameFileFolder_Click(object sender, RoutedEventArgs e)
        {
            // Check the selected game type to decide between file or folder browsing.
            string selectedGameType = (GameTypeComboBox.SelectedItem as string) ?? "Emulated";

            if (selectedGameType.Equals("FolderBased", StringComparison.OrdinalIgnoreCase))
            {
                // Use a FolderBrowserDialog for folder-based games.
                var dlg = new System.Windows.Forms.FolderBrowserDialog();
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    GamePathTextBox.Text = dlg.SelectedPath;
                }
            }
            else
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "All Files (*.*)|*.*"
                };
                if (dlg.ShowDialog() == true)
                {
                    GamePathTextBox.Text = dlg.FileName;
                }
            }
        }

        // Browse for Game Image is no longer needed because the cover is computed automatically.
        private void BrowseGameImage_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Cover image is auto-detected based on the game file location.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // "Add Game" Button Click: create a new game from form data.
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields: Name and Game file/folder.
            if (string.IsNullOrWhiteSpace(NameTextBox.Text)
                || string.IsNullOrWhiteSpace(GamePathTextBox.Text))
            {
                System.Windows.MessageBox.Show("Please fill in the name and game file.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Determine selected game type from the ComboBox.
            string selectedGameType = (GameTypeComboBox.SelectedItem as string) ?? "Emulated";

            // Create a new game instance based on the selected type.
            switch (selectedGameType)
            {
                case "Emulated":
                    var emGame = new EmulatedGame
                    {
                        // In EmulatedGame, set GamePath and associate an emulator.
                        GamePath = GamePathTextBox.Text,
                        EmulatorId = (EmulatorComboBox.SelectedItem as Emulator)?.Id
                    };
                    NewGame = emGame;
                    break;

                case "Native":
                    var nativeGame = new NativeGame
                    {
                        // For native games, set ExePath.
                        ExePath = GamePathTextBox.Text
                    };
                    NewGame = nativeGame;
                    break;

                case "FolderBased":
                    var folderGame = new FolderBasedGame
                    {
                        // For folder-based games, set FolderPath.
                        FolderPath = GamePathTextBox.Text,
                        EmulatorId = (EmulatorComboBox.SelectedItem as Emulator)?.Id
                    };
                    NewGame = folderGame;
                    break;

                default:
                    System.Windows.MessageBox.Show("Invalid game type selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
            }

            // Set common properties.
            NewGame.Name = NameTextBox.Text;
            NewGame.Console = ConsoleComboBox.SelectedItem?.ToString();
            NewGame.Genre = GenreComboBox.SelectedItem?.ToString();
            NewGame.ReleaseDate = ReleaseDatePicker.SelectedDate ?? DateTime.Now;

            DialogResult = true;
            Close();
        }

        private void GameTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (GameTypeComboBox.SelectedItem != null)
            {
                // Enable controls once a game type has been selected
                GamePathTextBox.IsEnabled = true;
                EmulatorComboBox.IsEnabled = true;
                ConsoleComboBox.IsEnabled = true;
                GenreComboBox.IsEnabled = true;
                ReleaseDatePicker.IsEnabled = true;
                BrowseGameFileFolderButton.IsEnabled = true;

                // Update the label based on the selected game type.
                // Note that your game types in code-behind are "Emulated", "Native", and "FolderBased".
                string selectedType = GameTypeComboBox.SelectedItem as string;
                if (selectedType.Equals("Emulated", StringComparison.OrdinalIgnoreCase))
                {
                    GameFileFolderLabel.Content = "Game File:";
                }
                else if (selectedType.Equals("Native", StringComparison.OrdinalIgnoreCase))
                {
                    GameFileFolderLabel.Content = "Game Executable:";
                }
                else
                {
                    // Default label content (e.g. for FolderBased games)
                    GameFileFolderLabel.Content = "Game Folder:";
                }

                // Check if the selected game type is Native.
                // If it is, hide the emulator controls.
                if (selectedType.Equals("Native", StringComparison.OrdinalIgnoreCase))
                {
                    EmulatorLabel.Visibility = Visibility.Collapsed;
                    EmulatorComboBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Otherwise, ensure they are visible.
                    EmulatorLabel.Visibility = Visibility.Visible;
                    EmulatorComboBox.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // If no game type is selected, disable the controls and revert the label.
                GamePathTextBox.IsEnabled = false;
                EmulatorComboBox.IsEnabled = false;
                ConsoleComboBox.IsEnabled = false;
                GenreComboBox.IsEnabled = false;
                ReleaseDatePicker.IsEnabled = false;
                BrowseGameFileFolderButton.IsEnabled = false;
                GameFileFolderLabel.Content = "Game File:";
            }
        }
    }
}
