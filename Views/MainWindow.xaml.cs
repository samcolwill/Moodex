using System.Windows;
using System.Windows.Controls;
using System.Linq;
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

            // find Scripts menu item
            var scriptsMenu = ctx.Items.OfType<MenuItem>()
                .FirstOrDefault(mi => (mi.Header as string)?.StartsWith("Scripts") == true);
            if (scriptsMenu == null) return;

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

            if (game.InputScripts.Count == 0)
            {
                // No scripts to show; don't add placeholder or separator
                return;
            }

            scriptsMenu.Items.Add(new Separator());

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
    }
}

