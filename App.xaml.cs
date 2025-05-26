using Microsoft.Extensions.DependencyInjection;
using SamsGameLauncher.Models;
using SamsGameLauncher.Services;
using SamsGameLauncher.ViewModels;
using SamsGameLauncher.ViewModels.Settings;
using SamsGameLauncher.Views;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace SamsGameLauncher
{
    public partial class App : System.Windows.Application
    {
        private ServiceProvider? _provider;

        protected override void OnStartup(StartupEventArgs e)
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

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
            services.AddTransient<ManageEmulatorsWindowViewModel>();
            services.AddTransient<ManageEmulatorsWindow>();
            services.AddTransient<EditEmulatorWindow>();

            services.AddTransient(sp =>
              new ManageEmulatorsWindowViewModel(
                sp.GetRequiredService<GameLibrary>(),
                sp.GetRequiredService<IDialogService>(),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "emulators.json")
              )
            );

            // (and any other VMs you resolve manually…)
            _provider = services.BuildServiceProvider();

            // set up the main window
            var mainWin = _provider.GetRequiredService<MainWindow>();
            mainWin.DataContext = _provider.GetRequiredService<MainWindowViewModel>();
            mainWin.Show();

            // ── launch DS4Windows if enabled ─────────────────────────────
            var settingsService = _provider.GetRequiredService<ISettingsService>();
            var settings = settingsService.Load();
            if (settings.LaunchDs4WindowsOnStartup)
            {
                var exeDir = AppContext.BaseDirectory;
                var ds4Path = Path.Combine(exeDir, "DS4Windows", "DS4Windows.exe");
                if (File.Exists(ds4Path))
                {
                    Process.Start(new ProcessStartInfo(ds4Path)
                    {
                        UseShellExecute = true,
                        // Arguments = "--minimized" // if you want CLI flags
                    });
                }
                else
                {
                    // optional: log warning that DS4Windows.exe wasn't found
                }
            }
        }
        protected override void OnExit(ExitEventArgs e)
        {
            // If DS4Windows was installed by this launcher, shut down any copies on exit
            var settings = _provider?.GetRequiredService<ISettingsService>().Load();
            if (settings?.IsDs4Installed == true)
            {
                foreach (var proc in Process.GetProcessesByName("DS4Windows"))
                {
                    try
                    {
                        proc.CloseMainWindow();
                        if (!proc.WaitForExit(2000))
                            proc.Kill();
                    }
                    catch
                    {
                        // ignore any failure to close/kill
                    }
                }
            }

            base.OnExit(e);
        }

        private void App_DispatcherUnhandledException(object sender,
                                                  System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Windows.MessageBox.Show(
                $"UI thread exception:\n{e.Exception}",
                "Unhandled Exception",
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;      // or false to let the app crash after the dialog
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            System.Windows.MessageBox.Show(
                $"Background exception:\n{ex}",
                "Unhandled Exception",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
