using System.Windows;
using SamsGameLauncher.Models;
using SamsGameLauncher.ViewModels;

namespace SamsGameLauncher.Views
{
    public partial class AddGameWindow : Window
    {
        public AddGameWindow(List<Emulator> availableEmulators)
        {
            InitializeComponent();
            DataContext = new AddGameWindowViewModel(availableEmulators);
        }
        public GameBase? NewGame => (DataContext as AddGameWindowViewModel)?.NewGame;
    }
}
