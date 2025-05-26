using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SamsGameLauncher.Models;
using SamsGameLauncher.Services;  // assuming IDialogService lives here

namespace SamsGameLauncher.ViewModels
{
    public class ManageEmulatorsWindowViewModel : ObservableObject
    {
        private readonly GameLibrary _gameLibrary;
        private readonly string _emuFilePath;
        private readonly IDialogService _dialogs;

        public ObservableCollection<Emulator> Emulators { get; }
        public IRelayCommand<Emulator> EditEmulatorCommand { get; }
        public IAsyncRelayCommand<Emulator> RemoveEmulatorCommand { get; }
        public IRelayCommand<Window> CloseCommand { get; }

        public ManageEmulatorsWindowViewModel(
            GameLibrary gameLibrary,
            IDialogService dialogs,
            string emuFilePath)
        {
            _gameLibrary = gameLibrary;
            _emuFilePath = emuFilePath;
            _dialogs = dialogs;

            Emulators = new ObservableCollection<Emulator>(
                _gameLibrary.Emulators);

            EditEmulatorCommand = new RelayCommand<Emulator>(OnEditEmulator);
            RemoveEmulatorCommand = new AsyncRelayCommand<Emulator>(OnRemoveEmulatorAsync);
            CloseCommand = new RelayCommand<Window>(w => w?.Close());
        }

        private void OnEditEmulator(Emulator emulator)
        {
            // this pops the Edit dialog and, on OK, mutates 'emulator' in place
            var edited = _dialogs.ShowEditEmulator(emulator);
            if (edited == null) return;

            // persist to disk
            _gameLibrary.SaveEmulators(_emuFilePath);

            // now _refresh_ the UI collection
            Emulators.Clear();
            foreach (var e in _gameLibrary.Emulators)
                Emulators.Add(e);
        }

        private async Task OnRemoveEmulatorAsync(Emulator emulator)
        {
            if (!await _dialogs.ShowConfirmationAsync(
                    $"Remove emulator '{emulator.Name}'?",
                    "Confirm Removal"))
                return;

            // 1) Remove from the UI collection
            Emulators.Remove(emulator);

            // 2) _Also_ remove from the underlying GameLibrary
            _gameLibrary.Emulators.Remove(emulator);

            // 3) Persist out to emulators.json
            _gameLibrary.SaveEmulators(_emuFilePath);
        }
    }
}
