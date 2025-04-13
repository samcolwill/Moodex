using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using SamsGameLauncher.Models;
using SamsGameLauncher.Constants;

namespace SamsGameLauncher
{
    public partial class EditGameWindow : Window
    {
        // The game being edited (passed in by reference)
        public Game EditedGame { get; private set; }

        // Lists for binding to ComboBoxes
        public List<string> Consoles { get; set; }
        public List<string> Genres { get; set; }
        public List<Emulator> Emulators { get; set; }

        // Constructor accepts the game to edit and available emulators.
        public EditGameWindow(Game gameToEdit, List<Emulator> availableEmulators)
        {
            InitializeComponent();

            // Load your predefined lists.
            // You might use a constants file like LauncherConstants.
            Consoles = LauncherConstants.Consoles;
            Genres = LauncherConstants.Genres;
            Emulators = availableEmulators;

            // Set the DataContext for binding.
            DataContext = this;

            // Store a reference to the game being edited.
            EditedGame = gameToEdit;

            // Prepopulate the controls with the game's current data.
            NameTextBox.Text = EditedGame.Name;
            // Emulator dropdown – bind to the existing list and select the one matching the game.
            EmulatorComboBox.ItemsSource = Emulators;
            EmulatorComboBox.SelectedItem = Emulators.FirstOrDefault(em => em.Id == EditedGame.EmulatorId);

            GamePathTextBox.Text = EditedGame.GamePath;
            GameCoverPathTextBox.Text = EditedGame.GameCoverPath;
            ConsoleComboBox.ItemsSource = Consoles;
            ConsoleComboBox.SelectedItem = EditedGame.Console;
            GenreComboBox.ItemsSource = Genres;
            GenreComboBox.SelectedItem = EditedGame.Genre;
            ReleaseDatePicker.SelectedDate = EditedGame.ReleaseDate;
        }

        // Browse for Game File.
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

        // Browse for Game Cover Image.
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

        // "Save Changes" button click: update the game.
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(NameTextBox.Text)
                || string.IsNullOrWhiteSpace(GamePathTextBox.Text)
                || string.IsNullOrWhiteSpace(GameCoverPathTextBox.Text))
            {
                MessageBox.Show("Please fill in the name, game file, and cover image.", "Missing Information",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Update the EditedGame with new values.
            EditedGame.Name = NameTextBox.Text;
            EditedGame.GamePath = GamePathTextBox.Text;
            EditedGame.GameCoverPath = GameCoverPathTextBox.Text;
            EditedGame.Console = ConsoleComboBox.SelectedItem?.ToString();
            EditedGame.Genre = GenreComboBox.SelectedItem?.ToString();
            EditedGame.ReleaseDate = ReleaseDatePicker.SelectedDate ?? DateTime.Now;
            EditedGame.EmulatorId = (EmulatorComboBox.SelectedItem as Emulator)?.Id;
            EditedGame.Emulator = Emulators.FirstOrDefault(e => e.Id == EditedGame.EmulatorId);

            DialogResult = true;
            Close();
        }
    }
}
