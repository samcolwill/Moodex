using SamsGameLauncher.Models;

namespace SamsGameLauncher.Services
{
    // Abstracts showing dialogs so ViewModels don't need to know about WPF Windows.
    public interface IDialogService
    {
        // Shows the "Add Game" dialog; returns the created GameBase or null if cancelled.
        GameBase? ShowAddGame(IEnumerable<Emulator> emulators);

        // Shows the "Edit Game" dialog for an existing game; returns the same GameBase if saved, or null.
        GameBase? ShowEditGame(GameBase game, IEnumerable<Emulator> emulators);

        // Shows the "Add Emulator" dialog; returns the created Emulator or null if cancelled.
        Emulator? ShowAddEmulator();

        // Shows the Settings Menu.
        void ShowSettings(string sectionName);
    }
}