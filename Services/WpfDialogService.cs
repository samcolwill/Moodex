using SamsGameLauncher.Models;
using SamsGameLauncher.Views;

namespace SamsGameLauncher.Services
{
    public class WpfDialogService : IDialogService
    {
        public GameBase? ShowAddGame(IEnumerable<Emulator> emulators)
        {
            // Show AddGameWindow and pass in available emulators
            var win = new AddGameWindow(emulators.ToList());
            // Return the newly created game if OK, otherwise null
            return win.ShowDialog() == true
                ? win.NewGame
                : null;
        }

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
    }
}
