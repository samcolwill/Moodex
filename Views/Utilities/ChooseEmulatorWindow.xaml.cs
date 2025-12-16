using System.Windows;
using Moodex.Models;

namespace Moodex.Views.Utilities
{
    public partial class ChooseEmulatorWindow : Window
    {
        public EmulatorInfo? SelectedEmulator { get; private set; }
        public EmulatorInfo EmulatorA { get; }
        public EmulatorInfo EmulatorB { get; }
        public string Message { get; }

        public ChooseEmulatorWindow(EmulatorInfo a, EmulatorInfo b, string consoleName)
        {
            EmulatorA = a;
            EmulatorB = b;
            Message = $"Two emulators are configured for {consoleName}. Please select an emulator:";

            InitializeComponent();
            DataContext = this;
        }

        private void SelectA_Click(object sender, RoutedEventArgs e)
        {
            SelectedEmulator = EmulatorA;
            DialogResult = true;
            Close();
        }

        private void SelectB_Click(object sender, RoutedEventArgs e)
        {
            SelectedEmulator = EmulatorB;
            DialogResult = true;
            Close();
        }
    }
}


