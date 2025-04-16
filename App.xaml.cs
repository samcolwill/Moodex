using Microsoft.Extensions.DependencyInjection;
using SamsGameLauncher.Services;
using SamsGameLauncher.ViewModels;
using SamsGameLauncher.Views;
using System;
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
            // register your VM
            services.AddTransient<MainWindowViewModel>();
            // (and any other VMs you resolve manually…)

            _provider = services.BuildServiceProvider();

            // set up the main window
            var mainWin = new MainWindow();
            mainWin.DataContext = _provider.GetRequiredService<MainWindowViewModel>();
            mainWin.Show();
        }
    }
}
