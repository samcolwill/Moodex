using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SamsGameLauncher.Commands;
using SamsGameLauncher.Services;

namespace SamsGameLauncher.ViewModels.Settings
{
    public class LibrarySettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;
        private bool _librarySetting;
        public bool LibrarySetting
        {
            get => _librarySetting;
            set { _librarySetting = value; RaisePropertyChanged(); }
        }

        public ICommand SaveCommand { get; }

        public LibrarySettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            // you could load a value here, e.g.
            // GeneralSetting = settingsService.Load().GeneralSetting;
            SaveCommand = new RelayCommand(_ => { /* stub, or persist */ });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string p = null!)
          => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

}