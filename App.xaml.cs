using Microsoft.Extensions.DependencyInjection;
using SamsGameLauncher.Services;
using SamsGameLauncher.ViewModels;
using SamsGameLauncher.ViewModels.Settings;
using SamsGameLauncher.Views;
using System.Windows;

namespace SamsGameLauncher
{
    public partial class App : System.Windows.Application
    {
        private ServiceProvider? _provider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            // register the dialog service
            services.AddSingleton<IDialogService, WpfDialogService>();
            services.AddSingleton<ISettingsService, JsonSettingsService>();
            services.AddSingleton<IWindowPlacementService, WindowPlacementService>();
            services.AddSingleton<IFileMoveService, FileMoveService>();

            // register your VM
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>();
            services.AddTransient<SettingsWindowViewModel>();
            services.AddTransient<SettingsWindow>();

            // (and any other VMs you resolve manually…)
            _provider = services.BuildServiceProvider();

            // set up the main window
            var mainWin = _provider.GetRequiredService<MainWindow>();
            mainWin.DataContext = _provider.GetRequiredService<MainWindowViewModel>();
            mainWin.Show();
        }
    }
}
