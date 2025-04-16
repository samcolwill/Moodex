using System.Windows;
using System.Windows.Input;
using SamsGameLauncher.Models;
using SamsGameLauncher.Commands;

namespace SamsGameLauncher.ViewModels
{
    // ViewModel for AddEmulatorWindow.xaml
    public class AddEmulatorWindowViewModel : BaseViewModel
    {
        // backing fields for input fields
        private string _id = "";
        private string _name = "";
        private string _executablePath = "";
        private string _defaultArguments = "";

        // bound to the "ID" TextBox
        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                RaisePropertyChanged();
                // update CanExecute for Save button
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }

        // bound to the "Name" TextBox
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                RaisePropertyChanged();
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }

        // bound to the "Executable Path" TextBox
        public string ExecutablePath
        {
            get => _executablePath;
            set
            {
                _executablePath = value;
                RaisePropertyChanged();
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }

        // bound to the "Arguments" TextBox (optional)
        public string DefaultArguments
        {
            get => _defaultArguments;
            set
            {
                _defaultArguments = value;
                RaisePropertyChanged();
            }
        }

        // holds the newly created Emulator after Save
        public Emulator? NewEmulator { get; private set; }

        // commands bound to buttons in the view
        public ICommand BrowseCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public AddEmulatorWindowViewModel()
        {
            // wire up Browse -> open file dialog
            BrowseCommand = new RelayCommand(p => ExecuteBrowse(p as Window));
            // Save only enabled when required fields are non-empty
            SaveCommand = new RelayCommand(p => ExecuteSave(p as Window), p => CanSave());
            // Cancel simply closes the window
            CancelCommand = new RelayCommand(p => (p as Window)?.Close());
        }

        // opens OpenFileDialog for selecting the EXE
        private void ExecuteBrowse(Window? owner)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
                ExecutablePath = dlg.FileName;
        }

        // determines whether SaveCommand can run
        private bool CanSave()
        {
            return
                !string.IsNullOrWhiteSpace(Id) &&
                !string.IsNullOrWhiteSpace(Name) &&
                !string.IsNullOrWhiteSpace(ExecutablePath);
        }

        // called when user clicks "Add Emulator"
        private void ExecuteSave(Window? owner)
        {
            // construct the new Emulator instance
            NewEmulator = new Emulator
            {
                Id = Id,
                Name = Name,
                ExecutablePath = ExecutablePath,
                DefaultArguments = DefaultArguments
            };

            // close dialog with success result
            if (owner != null)
            {
                owner.DialogResult = true;
                owner.Close();
            }
        }
    }
}
