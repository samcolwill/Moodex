using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
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

        public ObservableCollection<EmulatorInfo> Emulators { get; }
        public IRelayCommand<EmulatorInfo> OpenEmulatorCommand { get; }
        public IRelayCommand<EmulatorInfo> EditEmulatorCommand { get; }
        public IAsyncRelayCommand<EmulatorInfo> RemoveEmulatorCommand { get; }
        public IRelayCommand<Window> CloseCommand { get; }

        public ManageEmulatorsWindowViewModel(
            GameLibrary gameLibrary,
            IDialogService dialogs,
            string emuFilePath)
        {
            _gameLibrary = gameLibrary;
            _emuFilePath = emuFilePath;
            _dialogs = dialogs;

            Emulators = new ObservableCollection<EmulatorInfo>(
                _gameLibrary.Emulators);

            OpenEmulatorCommand = new RelayCommand<EmulatorInfo>(OnOpenEmulator);
            EditEmulatorCommand = new RelayCommand<EmulatorInfo>(OnEditEmulator);
            RemoveEmulatorCommand = new AsyncRelayCommand<EmulatorInfo>(OnRemoveEmulatorAsync);
            CloseCommand = new RelayCommand<Window>(w => w?.Close());
        }

        private void OnOpenEmulator(EmulatorInfo emulator)
        {
            try
            {
                var psi = new ProcessStartInfo(emulator.ExecutablePath)
                {
                    UseShellExecute = true,
                    Arguments = emulator.DefaultArguments
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error launching '{emulator.Name}':\n{ex.Message}",
                    "Launch Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnEditEmulator(EmulatorInfo emulator)
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

        private async Task OnRemoveEmulatorAsync(EmulatorInfo emulator)
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
