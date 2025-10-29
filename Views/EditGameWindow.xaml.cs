using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Windows;
using Moodex.Models;
using Moodex.Services;
using Moodex.ViewModels;

namespace Moodex.Views
{
    public partial class EditGameWindow : Window
    {
        [SupportedOSPlatform("windows")]
        public EditGameWindow(GameInfo gameToEdit)
        {
            InitializeComponent();

            var settingsService = new JsonSettingsService();

            DataContext = new EditGameWindowViewModel(gameToEdit, settingsService);
        }
    }
}

