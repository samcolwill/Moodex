using System.Windows;
using Moodex.Models;
using Moodex.Services;
using Moodex.ViewModels;

namespace Moodex.Views
{
    public partial class AddGameWindow : Window
    {
        public AddGameWindow()
        {
            InitializeComponent();

            var settingsService = new JsonSettingsService();

            DataContext = new AddGameWindowViewModel(settingsService);
        }
        public GameInfo? NewGame => (DataContext as AddGameWindowViewModel)?.NewGame;
    }
}

