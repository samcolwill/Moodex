using System.Windows;
using SamsGameLauncher.Models;
using SamsGameLauncher.Services;
using SamsGameLauncher.ViewModels;

namespace SamsGameLauncher.Views
{
    public partial class AddGameWindow : Window
    {
        public AddGameWindow(List<Emulator> availableEmulators)
        {
            InitializeComponent();

            var settingsService = new JsonSettingsService();

            DataContext = new AddGameWindowViewModel(availableEmulators, settingsService);
        }
        public GameBase? NewGame => (DataContext as AddGameWindowViewModel)?.NewGame;
    }
}
