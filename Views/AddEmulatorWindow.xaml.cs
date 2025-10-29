using System.Windows;
using Moodex.Models;
using Moodex.ViewModels;

namespace Moodex.Views
{
    public partial class AddEmulatorWindow : Window
    {
        public AddEmulatorWindow()
        {
            InitializeComponent();
        }
        public EmulatorInfo? NewEmulator => (DataContext as AddEmulatorWindowViewModel)?.NewEmulator;
    }
}
