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
        public Game NewGame { get; private set; }

        public List<string> Consoles { get; set; }
        public List<string> Genres { get; set; }
        public List<Emulator> Emulators { get; set; }

        public AddGameWindow(List<Emulator> availableEmulators)
        {
            InitializeComponent();

            // Get predefined consoles and genres from our constants
            Consoles = LauncherConstants.Consoles;
            Genres = LauncherConstants.Genres;
            Emulators = availableEmulators; // pass in from MainWindow

            // Set DataContext for binding in the window
            DataContext = this;
        }

        // Browse for Game File
        private void BrowseGameFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                GamePathTextBox.Text = dlg.FileName;
            }
        }

        // Browse for Game Image
        private void BrowseGameImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
            };
            if (dlg.ShowDialog() == true)
            {
                GameCoverPathTextBox.Text = dlg.FileName;
            }
        }

        // "Add Game" Button Click: create a new game from form data
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields (Name, GameFilePath, GameImagePath)
            if (string.IsNullOrWhiteSpace(NameTextBox.Text)
                || string.IsNullOrWhiteSpace(GamePathTextBox.Text)
                || string.IsNullOrWhiteSpace(GameCoverPathTextBox.Text))
            {
                MessageBox.Show("Please fill in the name, game file, and image file.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewGame = new Game
            {
                Name = NameTextBox.Text,
                EmulatorId = (EmulatorComboBox.SelectedItem as Emulator)?.Id,
                GamePath = GamePathTextBox.Text,
                GameCoverPath = GameCoverPathTextBox.Text,
                Console = ConsoleComboBox.SelectedItem?.ToString(),
                Genre = GenreComboBox.SelectedItem?.ToString(),
                ReleaseDate = ReleaseDatePicker.SelectedDate ?? DateTime.Now
            };

            DialogResult = true;
            Close();
        }
    }
}
