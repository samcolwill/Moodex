using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.IO;
using Moodex.Models;
using Moodex.ViewModels;

namespace Moodex.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void GameContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu ctx) return;
            if (ctx.PlacementTarget is not FrameworkElement fe) return;
            if (fe.DataContext is not GameInfo game) return;
            if (DataContext is not MainWindowViewModel vm) return;

            // find Scripts menu item (optional)
            var scriptsMenu = ctx.Items.OfType<MenuItem>()
                .FirstOrDefault(mi => (mi.Header as string)?.StartsWith("Scripts") == true);

            if (scriptsMenu != null)
            {
                // ensure our context menu item style applies to children
                if (TryFindResource("ContextMenuItemStyle") is Style ctxItemStyle)
                {
                    scriptsMenu.ItemContainerStyle = ctxItemStyle;
                }

                scriptsMenu.Items.Clear();

                // Add Script
                var add = new MenuItem { Header = "Add Script" };
                add.Command = vm.CreateScriptCommand;
                add.CommandParameter = game;
                scriptsMenu.Items.Add(add);
            }

            if (scriptsMenu != null && game.InputScripts.Count > 0)
            {
                // Build per-script nested menus
                foreach (var s in game.InputScripts)
                {
                    var root = new MenuItem { Header = new TextBlock { Text = s.Name } };
                    if (TryFindResource("ContextMenuItemStyle") is Style itemStyle)
                    {
                        root.ItemContainerStyle = itemStyle; // ensure grandchildren open to the right and show glyph
                    }

                    var toggle = new MenuItem { Header = s.Enabled ? "Disable Script" : "Enable Script" };
                    toggle.Command = vm.ToggleScriptEnabledByNameCommand;
                    toggle.CommandParameter = $"{game.GameGuid}|{s.Name}";
                    root.Items.Add(toggle);

                    var edit = new MenuItem { Header = "Edit Script" };
                    edit.Command = vm.EditScriptByNameCommand;
                    edit.CommandParameter = $"{game.GameGuid}|{s.Name}";
                    root.Items.Add(edit);

                    var del = new MenuItem { Header = "Delete Script" };
                    del.Command = vm.DeleteScriptByNameCommand;
                    del.CommandParameter = $"{game.GameGuid}|{s.Name}";
                    root.Items.Add(del);

                    scriptsMenu.Items.Add(root);
                }
            }

            // Build Controller submenu
            var controllerMenu = ctx.Items.OfType<MenuItem>()
                .FirstOrDefault(mi => (mi.Header as string)?.StartsWith("Controller") == true);
            if (controllerMenu != null)
            {
                controllerMenu.Items.Clear();
                var toggle = new MenuItem { Header = game.ControllerEnabled ? "Disable" : "Enable" };
                if (DataContext is MainWindowViewModel vm2)
                {
                    toggle.Command = vm2.ToggleControllerCommand;
                    toggle.CommandParameter = game;
                }
                controllerMenu.Items.Add(toggle);

                var cfg = new MenuItem { Header = "Configure Profile" };
                if (DataContext is MainWindowViewModel vm3)
                {
                    cfg.Command = vm3.ConfigureControllerProfileCommand;
                    cfg.CommandParameter = game;
                }
                controllerMenu.Items.Add(cfg);

                // Delete Profile (only if exists)
                if (DataContext is MainWindowViewModel vm4)
                {
                    var perGamePath = string.IsNullOrEmpty(game.GameRootPath) ? null : Path.Combine(game.GameRootPath, "input", "ds4windows_controller_profile.xml");
                    if (perGamePath != null && File.Exists(perGamePath))
                    {
                        var del = new MenuItem { Header = "Delete Profile" };
                        del.Command = vm4.DeleteControllerProfileCommand;
                        del.CommandParameter = game;
                        controllerMenu.Items.Add(del);
                    }
                }
            }
        }
    }
}

