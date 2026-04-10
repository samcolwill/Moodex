using Microsoft.Extensions.DependencyInjection;
using Moodex.Models;
using Moodex.Services;
using Moodex.Services.Igdb;
using Moodex.Services.Steam;
using Moodex.Utilities;
using Moodex.ViewModels;
using Moodex.ViewModels.Settings;
using Moodex.Views;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Moodex
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
            // Removed FileMoveService - archive/restore handled by ArchiveService
            services.AddSingleton<IAutoHotKeyScriptService, AutoHotKeyScriptService>();
            services.AddSingleton<ISteamDetectionService, SteamDetectionService>();
            services.AddSingleton<ISteamImportService, SteamImportService>();
            services.AddSingleton<ISteamLaunchService, SteamLaunchService>();
            services.AddSingleton<ISteamWebApiService, SteamWebApiService>();
            services.AddSingleton<IIgdbService, IgdbService>();

            // register your VM
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>();
            services.AddTransient<SettingsWindowViewModel>();
            services.AddTransient<SettingsWindow>();
            services.AddTransient<ManageEmulatorsWindowViewModel>();
            services.AddTransient<ManageEmulatorsWindow>();
            services.AddTransient<AddEmulatorWindowViewModel>();
            services.AddTransient<AddEmulatorWindow>();
            services.AddTransient<EditEmulatorWindowViewModel>();
            services.AddTransient<EditEmulatorWindow>();
            services.AddTransient<Moodex.ViewModels.Help.GettingStartedWindowViewModel>();
            services.AddTransient<Moodex.Views.Help.GettingStartedWindow>();

            services.AddSingleton<ILibraryScanner, LibraryScanner>();
            services.AddSingleton<IArchiveService, ArchiveService>();
            services.AddSingleton<MoodexState>(sp =>
            {
                var settingsService = sp.GetRequiredService<ISettingsService>();
                var scanner = sp.GetRequiredService<ILibraryScanner>();
                var settings = settingsService.Load();
                var state = new MoodexState();
                var (emus, games) = scanner.Scan(settings.ActiveLibraryPath);
                state.Emulators = new System.Collections.ObjectModel.ObservableCollection<Models.EmulatorInfo>(emus);
                state.Games = new System.Collections.ObjectModel.ObservableCollection<Models.GameInfo>(games);
                return state;
            });

            services.AddTransient(sp =>
              new ManageEmulatorsWindowViewModel(
                sp.GetRequiredService<MoodexState>(),
                sp.GetRequiredService<IDialogService>()
              )
            );

            // (and any other VMs you resolve manually…)
            _provider = services.BuildServiceProvider();

            // Configure settings
            var settingsService = _provider.GetRequiredService<ISettingsService>();
            var settings = settingsService.Load();

            // ── refresh the ConsoleRegistry so GameInfo.ConsoleName will work ─────────────
            ConsoleRegistry.Refresh(settingsService);

            // ── Auto-scan Steam library on startup if configured ────────────
            if (settings.Steam.Enabled && settings.Steam.AutoScanOnStartup
                && !string.IsNullOrWhiteSpace(settings.Steam.SteamApiKey)
                && !string.IsNullOrWhiteSpace(settings.Steam.SteamId64)
                && settings.Steam.LibraryPaths.Count > 0)
            {
                try
                {
                    var steamDetection = _provider.GetRequiredService<ISteamDetectionService>();
                    var steamImport = _provider.GetRequiredService<ISteamImportService>();
                    var steamWebApi = _provider.GetRequiredService<ISteamWebApiService>();

                    var ownedGames = Task.Run(() => steamWebApi.GetOwnedGamesAsync(
                        settings.Steam.SteamApiKey, settings.Steam.SteamId64)).GetAwaiter().GetResult();

                    if (ownedGames.Count > 0)
                    {
                        var allGames = steamDetection.ResolveInstallStatus(ownedGames, settings.Steam.LibraryPaths);
                        var steamRoot = !string.IsNullOrEmpty(settings.Steam.SteamExePath) ? Path.GetDirectoryName(settings.Steam.SteamExePath) : null;
                        steamImport.ImportSteamGames(settings.ActiveLibraryPath, allGames, steamRoot, settings.Steam.IgnoredGames);
                    }
                }
                catch { }
            }

            // set up the main window
            var mainWin = _provider.GetRequiredService<MainWindow>();
            mainWin.DataContext = _provider.GetRequiredService<MainWindowViewModel>();
            mainWin.Show();
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

