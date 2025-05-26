using SamsGameLauncher.Models;

namespace SamsGameLauncher.Services
{
    // Abstracts showing dialogs so ViewModels don't need to know about WPF Windows.
    public interface IDialogService
    {
        // Shows the "Add Game" dialog; returns the created GameBase or null if cancelled.
        GameBase? ShowAddGame();

        // Shows the "Edit Game" dialog for an existing game; returns the same GameBase if saved, or null.
        GameBase? ShowEditGame(GameBase game);

        // Shows the "Add Emulator" dialog; returns the created Emulator or null if cancelled.
        Emulator? ShowAddEmulator();
        Emulator? ShowEditEmulator(Emulator e);

        // Shows the Manage Emulators Menu.
        void ShowManageEmulators();

        // Shows the Settings Menu.
        void ShowSettings(string sectionName);
        // Shows the About Menu.
        void ShowAbout();
        // Shows confirmation dialog.
        Task<bool> ShowConfirmationAsync(string title, string message);
    }
}