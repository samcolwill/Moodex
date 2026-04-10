using Microsoft.Extensions.DependencyInjection;
using Moodex.Models;
using Moodex.Services.Igdb;
using Moodex.ViewModels;
using Moodex.ViewModels.Help;
using Moodex.ViewModels.Settings;
using Moodex.Views;
using Moodex.Views.Help;
using System.Runtime.Versioning;
using System.Windows;

namespace Moodex.Services
{
    public class WpfDialogService : IDialogService
    {
        private readonly IServiceProvider _provider;

        // inject the container
        public WpfDialogService(IServiceProvider provider)
        {
            _provider = provider;
        }

        public GameInfo? ShowAddGame()
        {
            var igdb = _provider.GetRequiredService<IIgdbService>();
            var win = new AddGameWindow(igdb);
            return win.ShowDialog() == true
                ? win.NewGame
                : null;
        }

        [SupportedOSPlatform("windows")]
        public GameInfo? ShowEditGame(GameInfo game)
        {
            var igdb = _provider.GetRequiredService<IIgdbService>();
            var win = new EditGameWindow(game, igdb);
            return win.ShowDialog() == true
                ? game
                : null;
        }

        public EmulatorInfo? ShowAddEmulator()
        {
            // resolve via DI so that the VM ctor with dependencies is used
            var win = _provider.GetRequiredService<AddEmulatorWindow>();
            var vm = _provider.GetRequiredService<AddEmulatorWindowViewModel>();
            win.DataContext = vm;
            win.Owner = System.Windows.Application.Current.MainWindow;

            return win.ShowDialog() == true
                ? win.NewEmulator
                : null;
        }

        public EmulatorInfo? ShowEditEmulator(EmulatorInfo toEdit)
        {
            var settingsService = _provider.GetRequiredService<ISettingsService>();
            // manually new up the ViewModel, passing the selected Emulator
            var vm = new EditEmulatorWindowViewModel(settingsService, toEdit);
            // then new up the window and give it the VM
            var win = new EditEmulatorWindow(vm);
            win.Owner = System.Windows.Application.Current.MainWindow;
            var result = win.ShowDialog();
            if (result == true)
            {
                // persist emulator manifest
                try
                {
                    var settings = settingsService.Load();
                    var root = System.IO.Path.Combine(settings.ActiveLibraryPath, "Emulators");
                    System.IO.Directory.CreateDirectory(root);
                    var dir = System.IO.Path.Combine(root, toEdit.Id);
                    System.IO.Directory.CreateDirectory(dir);
                    if (string.IsNullOrWhiteSpace(toEdit.Guid))
                    {
                        toEdit.Guid = System.Guid.NewGuid().ToString();
                    }
                    var man = new Moodex.Models.Manifests.EmulatorManifest
                    {
                        Name = toEdit.Name,
                        Id = toEdit.Id,
                        Guid = toEdit.Guid,
                        EmulatedConsoleIds = toEdit.EmulatedConsoleIds ?? new System.Collections.Generic.List<string>(),
                        ExecutablePath = toEdit.ExecutablePath,
                        DefaultArguments = toEdit.DefaultArguments
                    };
                    var json = System.Text.Json.JsonSerializer.Serialize(man, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(System.IO.Path.Combine(dir, ".moodex_emulator"), json);
                }
                catch { }
                return toEdit;
            }
            return null;
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

        public void ShowGettingStarted()
        {
            var vm = _provider.GetRequiredService<Moodex.ViewModels.Help.GettingStartedWindowViewModel>();
            var win = _provider.GetRequiredService<Moodex.Views.Help.GettingStartedWindow>();
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

        public void ShowAchievements(GameInfo game)
        {
            var win = new Moodex.Views.Utilities.AchievementsViewerWindow(game)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            win.ShowDialog();
        }

        public void ShowAddAchievement(GameInfo game)
        {
            var win = new Moodex.Views.Utilities.AddAchievementWindow(game)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            win.ShowDialog();
        }

        public void ShowManageAchievements(GameInfo game)
        {
            var win = new Moodex.Views.Utilities.ManageAchievementsWindow(game)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            win.ShowDialog();
        }

        public EmulatorInfo? ChooseEmulatorForConsole(string consoleId, IReadOnlyList<EmulatorInfo> candidates)
        {
            if (candidates == null || candidates.Count == 0) return null;
            var consoleName = Moodex.Utilities.ConsoleRegistry.GetDisplayName(consoleId) ?? consoleId;

            // Three or more: show error with bullet list
            if (candidates.Count >= 3)
            {
                var list = string.Join("\n - ", candidates.Select(c => $"{c.Name} ({System.IO.Path.GetFileName(c.ExecutablePath)})"));
                System.Windows.MessageBox.Show(
                    $"Too many emulators are configured for {consoleName}. Please keep only two or fewer.\n\nConfigured emulators:\n - {list}",
                    "Multiple Emulators",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return null;
            }

            if (candidates.Count == 1) return candidates[0];

            // Exactly two: show chooser with icons
            var chooser = new Moodex.Views.Utilities.ChooseEmulatorWindow(candidates[0], candidates[1], consoleName)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            var result = chooser.ShowDialog();
            if (result == true)
            {
                return chooser.SelectedEmulator;
            }
            return null;
        }
    }
}

