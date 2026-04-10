using System.Windows;
using Moodex.Models;
using Moodex.Services;
using Moodex.Services.Igdb;
using Moodex.ViewModels;

namespace Moodex.Views
{
    public partial class AddGameWindow : Window
    {
        public AddGameWindow(IIgdbService igdbService)
        {
            InitializeComponent();

            var settingsService = new JsonSettingsService();

            DataContext = new AddGameWindowViewModel(settingsService, igdbService);
        }
        public GameInfo? NewGame => (DataContext as AddGameWindowViewModel)?.NewGame;
    }
}
