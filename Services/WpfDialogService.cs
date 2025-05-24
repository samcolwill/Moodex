using Microsoft.Extensions.DependencyInjection;
using SamsGameLauncher.Models;
using SamsGameLauncher.ViewModels;
using SamsGameLauncher.ViewModels.Help;
using SamsGameLauncher.ViewModels.Settings;
using SamsGameLauncher.Views;
using SamsGameLauncher.Views.Help;
using System.Runtime.Versioning;
using System.Windows;

namespace SamsGameLauncher.Services
{
    public class WpfDialogService : IDialogService
    {
        private readonly IServiceProvider _provider;

        // inject the container
        public WpfDialogService(IServiceProvider provider)
        {
            _provider = provider;
        }

        public GameBase? ShowAddGame(IEnumerable<Emulator> emulators)
        {
            // Show AddGameWindow and pass in available emulators
            var win = new AddGameWindow(emulators.ToList());
            // Return the newly created game if OK, otherwise null
            return win.ShowDialog() == true
                ? win.NewGame
                : null;
        }

        [SupportedOSPlatform("windows")]
        public GameBase? ShowEditGame(GameBase game, IEnumerable<Emulator> emulators)
        {
            // Show EditGameWindow for the selected game
            var win = new EditGameWindow(game, emulators.ToList());
            // If saved, return the same GameBase (updated in place), else null
            return win.ShowDialog() == true
                ? game
                : null;
        }

        public Emulator? ShowAddEmulator()
        {
            // Show AddEmulatorWindow
            var win = new AddEmulatorWindow();
            // Return the created emulator if OK, otherwise null
            return win.ShowDialog() == true
                ? win.NewEmulator
                : null;
        }

        public void ShowSettings(string sectionName)
        {
            // 1) resolve the VM & Window
            var vm = _provider.GetRequiredService<SettingsWindowViewModel>();
            var win = _provider.GetRequiredService<SettingsWindow>();

            // 2) pick the right section
            var found = vm.Sections.FirstOrDefault(s => s.Name == sectionName);
            if (found != null) vm.SelectedSection = found;

            // 3) show
            win.DataContext = vm;
            win.Owner = System.Windows.Application.Current.MainWindow;
            win.ShowDialog();
        }

        public void ShowAbout()
        {
            var vm = new AboutViewModel();
            var win = new AboutWindow
            {
                DataContext = vm,
                Owner = System.Windows.Application.Current.MainWindow
            };
            win.ShowDialog();
        }

        public Task<bool> ShowConfirmationAsync(string title, string message)
        {
            var result = System.Windows.MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            return Task.FromResult(result == MessageBoxResult.Yes);
        }
    }
}
