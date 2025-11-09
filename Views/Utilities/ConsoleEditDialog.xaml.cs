using System;
using System.Windows;

namespace Moodex.Views.Utilities
{
    public partial class ConsoleEditDialog : Window
    {
        public string ConsoleId
        {
            get => IdBox.Text.Trim();
            set => IdBox.Text = value;
        }
        public string ConsoleName
        {
            get => NameBox.Text.Trim();
            set => NameBox.Text = value;
        }
        public double AspectW
        {
            get => double.TryParse(AspectWBox.Text.Trim(), out var n) ? n : 1.0;
            set => AspectWBox.Text = value.ToString();
        }
        public double AspectH
        {
            get => double.TryParse(AspectHBox.Text.Trim(), out var n) ? n : 1.0;
            set => AspectHBox.Text = value.ToString();
        }

        public ConsoleEditDialog()
        {
            InitializeComponent();
            // defaults
            AspectW = 1;
            AspectH = 1;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ConsoleId) || string.IsNullOrWhiteSpace(ConsoleName))
            {
                System.Windows.MessageBox.Show("Please provide a Console ID and Name.", "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }
            if (AspectW <= 0 || AspectH <= 0)
            {
                System.Windows.MessageBox.Show("Aspect ratio must be positive numbers.", "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }
            DialogResult = true;
            Close();
        }
    }
}


