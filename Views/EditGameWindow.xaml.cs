using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Windows;
using SamsGameLauncher.Models;
using SamsGameLauncher.Services;
using SamsGameLauncher.ViewModels;

namespace SamsGameLauncher.Views
{
    public partial class EditGameWindow : Window
    {
        [SupportedOSPlatform("windows")]
        public EditGameWindow(GameBase gameToEdit)
        {
            InitializeComponent();

            var settingsService = new JsonSettingsService();

            DataContext = new EditGameWindowViewModel(gameToEdit, settingsService);
        }
    }
}
