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

        public GameBase? ShowAddGame()
        {
            // Show AddGameWindow and pass in available emulators
            var win = new AddGameWindow();
            // Return the newly created game if OK, otherwise null
            return win.ShowDialog() == true
                ? win.NewGame
                : null;
        }

        [SupportedOSPlatform("windows")]
        public GameBase? ShowEditGame(GameBase game)
        {
            // Show EditGameWindow for the selected game
            var win = new EditGameWindow(game);
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

        public Emulator? ShowEditEmulator(Emulator toEdit)
        {
            // manually new up the ViewModel, passing the selected Emulator
            var vm = new EditEmulatorWindowViewModel(toEdit);
            // then new up the window and give it the VM
            var win = new EditEmulatorWindow(vm);
            win.Owner = System.Windows.Application.Current.MainWindow;
            var result = win.ShowDialog();
            return result == true ? toEdit : null;
        }

        public void ShowManageEmulators()
        {
            // resolve via DI so that ManageEmulatorsWindow
            // gets its ViewModel injected automatically
            var win = _provider.GetRequiredService<ManageEmulatorsWindow>();
            win.Owner = System.Windows.Application.Current.MainWindow;
            win.ShowDialog();
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
