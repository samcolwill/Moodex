using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SamsGameLauncher.Commands;
using SamsGameLauncher.Services;

namespace SamsGameLauncher.ViewModels.Settings
{
    public class GeneralSettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;
        private bool _generalSetting;
        public bool GeneralSetting
        {
            get => _generalSetting;
            set { _generalSetting = value; RaisePropertyChanged(); }
        }

        public ICommand SaveCommand { get; }

        public GeneralSettingsViewModel(ISettingsService settingsService)
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