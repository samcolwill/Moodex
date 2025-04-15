using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Win32;
using SamsGameLauncher.Models;
using SamsGameLauncher.Constants;

namespace SamsGameLauncher
{
    public partial class EditGameWindow : Window
    {
        // The game being edited is now of type GameBase.
        public GameBase EditedGame { get; private set; }

        public List<string> Consoles { get; set; }
        public List<string> Genres { get; set; }
        public List<Emulator> Emulators { get; set; }
        // Display the game type (read-only).
        public string GameTypeName { get; set; }

        public EditGameWindow(GameBase gameToEdit, List<Emulator> availableEmulators)
        {
            InitializeComponent();

            Consoles = LauncherConstants.Consoles;
            Genres = LauncherConstants.Genres;
            Emulators = availableEmulators;

            DataContext = this;

            EditedGame = gameToEdit;
            GameTypeName = EditedGame.GameType.ToString();

            // Prepopulate common fields.
            NameTextBox.Text = EditedGame.Name;
            ConsoleComboBox.ItemsSource = Consoles;
            ConsoleComboBox.SelectedItem = EditedGame.Console;
            GenreComboBox.ItemsSource = Genres;
            GenreComboBox.SelectedItem = EditedGame.Genre;
            ReleaseDatePicker.SelectedDate = EditedGame.ReleaseDate;

            // Populate file path field and emulator selection based on game type.
            if (EditedGame is EmulatedGame emGame)
            {
                GamePathTextBox.Text = emGame.GamePath;
                EmulatorComboBox.ItemsSource = Emulators;
                EmulatorComboBox.SelectedItem = Emulators.FirstOrDefault(e => e.Id == emGame.EmulatorId);
            }
            else if (EditedGame is NativeGame nativeGame)
            {
                GamePathTextBox.Text = nativeGame.ExePath;
                // Disable emulator selection for native games.
                EmulatorComboBox.IsEnabled = false;
            }
            else if (EditedGame is FolderBasedGame folderGame)
            {
                GamePathTextBox.Text = folderGame.FolderPath;
                EmulatorComboBox.IsEnabled = false;
            }
        }

        // Browse for Game File.
        private void BrowseGameFile_Click(object sender, RoutedEventArgs e)
        {
            // If the game is folder-based, use a folder browser.
            if (EditedGame is FolderBasedGame)
            {
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

        // Browse for Game Cover is not needed.
        private void BrowseGameImage_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Cover image is auto-detected based on the game file location.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // "Save Changes" button click: update the game.
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields.
            if (string.IsNullOrWhiteSpace(NameTextBox.Text)
                || string.IsNullOrWhiteSpace(GamePathTextBox.Text))
            {
                System.Windows.MessageBox.Show("Please fill in the name and game file.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Update common properties.
            EditedGame.Name = NameTextBox.Text;
            EditedGame.Console = ConsoleComboBox.SelectedItem?.ToString();
            EditedGame.Genre = GenreComboBox.SelectedItem?.ToString();
            EditedGame.ReleaseDate = ReleaseDatePicker.SelectedDate ?? DateTime.Now;

            // Update based on game type.
            if (EditedGame is EmulatedGame emGame)
            {
                emGame.GamePath = GamePathTextBox.Text;
                emGame.EmulatorId = (EmulatorComboBox.SelectedItem as Emulator)?.Id;
                emGame.Emulator = Emulators.FirstOrDefault(e => e.Id == emGame.EmulatorId);
            }
            else if (EditedGame is NativeGame nativeGame)
            {
                nativeGame.ExePath = GamePathTextBox.Text;
            }
            else if (EditedGame is FolderBasedGame folderGame)
            {
                folderGame.FolderPath = GamePathTextBox.Text;
            }

            DialogResult = true;
            Close();
        }
    }
}
