using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SamsGameLauncher.Services;
using CommunityToolkit.Mvvm.Input;

namespace SamsGameLauncher.ViewModels.Settings
{
    public class ControlsSettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;
        private bool _controlsSetting;
        public bool ControlsSetting
        {
            get => _controlsSetting;
            set { _controlsSetting = value; RaisePropertyChanged(); }
        }

        public IRelayCommand SaveCommand { get; }

        public ControlsSettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            // you could load a value here, e.g.
            // GeneralSetting = settingsService.Load().GeneralSetting;
            SaveCommand = new RelayCommand(Save);
        }

        private void Save()
        {
            // TODO: persist setting
            // var cfg = _settingsService.Load();
            // cfg.SomeBool = LibrarySetting;
            // _settingsService.Save(cfg);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string p = null!)
          => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}