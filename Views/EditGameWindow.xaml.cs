using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Windows;
using SamsGameLauncher.Models;
using SamsGameLauncher.ViewModels;

namespace SamsGameLauncher.Views
{
    public partial class EditGameWindow : Window
    {
        [SupportedOSPlatform("windows")]
        public EditGameWindow(GameBase gameToEdit, List<Emulator> availableEmulators)
        {
            InitializeComponent();
            DataContext = new EditGameWindowViewModel(gameToEdit, availableEmulators);
        }
    }
}
