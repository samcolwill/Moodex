using System;
using System.Windows;
using Microsoft.Win32;
using SamsGameLauncher.Models;

namespace SamsGameLauncher
{
    public partial class AddEmulatorWindow : Window
    {
        public Emulator NewEmulator { get; private set; }

        public AddEmulatorWindow()
        {
            InitializeComponent();
        }

        private void BrowseExecutable_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                ExecutablePathTextBox.Text = dlg.FileName;
            }
        }

        private void AddEmulatorButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate that required fields (ID, Name, Executable) are provided.
            if (string.IsNullOrWhiteSpace(IdTextBox.Text) ||
                string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                string.IsNullOrWhiteSpace(ExecutablePathTextBox.Text))
            {
                System.Windows.MessageBox.Show("Please fill in the ID, Name, and Executable Path.", "Missing Information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewEmulator = new Emulator
            {
                Id = IdTextBox.Text,
                Name = NameTextBox.Text,
                ExecutablePath = ExecutablePathTextBox.Text,
                DefaultArguments = DefaultArgumentsTextBox.Text  // Optional; may be empty
            };

            DialogResult = true;
            Close();
        }
    }
}
