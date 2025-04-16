using System.Windows;
using SamsGameLauncher.Models;
using SamsGameLauncher.ViewModels;

namespace SamsGameLauncher.Views
{
    public partial class AddEmulatorWindow : Window
    {
        public AddEmulatorWindow()
        {
            InitializeComponent();
        }
        public Emulator? NewEmulator => (DataContext as AddEmulatorWindowViewModel)?.NewEmulator;
    }
}