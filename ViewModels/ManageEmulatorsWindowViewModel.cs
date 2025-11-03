using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moodex.Models;
using Moodex.Services;  // assuming IDialogService lives here

namespace Moodex.ViewModels
{
    public class ManageEmulatorsWindowViewModel : ObservableObject
    {
        private readonly MoodexState _moodexState;
        private readonly IDialogService _dialogs;

        public ObservableCollection<EmulatorInfo> Emulators { get; }
        public IRelayCommand<EmulatorInfo> OpenEmulatorCommand { get; }
        public IRelayCommand<EmulatorInfo> EditEmulatorCommand { get; }
        public IAsyncRelayCommand<EmulatorInfo> RemoveEmulatorCommand { get; }
        public IRelayCommand<Window> CloseCommand { get; }

        public ManageEmulatorsWindowViewModel(
            MoodexState moodexState,
            IDialogService dialogs)
        {
            _moodexState = moodexState;
            _dialogs = dialogs;

            Emulators = _moodexState.Emulators;

            OpenEmulatorCommand = new RelayCommand<EmulatorInfo>(OnOpenEmulator);
            EditEmulatorCommand = new RelayCommand<EmulatorInfo>(OnEditEmulator);
            RemoveEmulatorCommand = new AsyncRelayCommand<EmulatorInfo>(OnRemoveEmulatorAsync);
            CloseCommand = new RelayCommand<Window>(w => w?.Close());
        }

        private void OnOpenEmulator(EmulatorInfo? emulator)
        {
            if (emulator == null) return;
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

        private void OnEditEmulator(EmulatorInfo? emulator)
        {
            if (emulator == null) return;
            // this pops the Edit dialog and, on OK, mutates 'emulator' in place
            var edited = _dialogs.ShowEditEmulator(emulator);
            if (edited == null) return;

            // collection is bound directly to state; it will reflect any changes
        }

        private async Task OnRemoveEmulatorAsync(EmulatorInfo? emulator)
        {
            if (emulator == null) return;
            if (!await _dialogs.ShowConfirmationAsync(
                    $"Remove emulator '{emulator.Name}'?",
                    "Confirm Removal"))
                return;

            // 1) Remove from the UI collection
            Emulators.Remove(emulator);

            // 2) _Also_ remove from the underlying MoodexState
            _moodexState.Emulators.Remove(emulator);

            // 3) Manifest-driven flow: persistence handled elsewhere (if needed)
        }
    }
}

