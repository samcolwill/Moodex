using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SamsGameLauncher.Services;
using CommunityToolkit.Mvvm.Input;
using System.Runtime.Intrinsics.Arm;

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

        public IRelayCommand SaveCommand { get; }

        public LibrarySettingsViewModel(ISettingsService settingsService)
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