using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Moodex.Services;
using Moodex.Services.Igdb;
using Moodex.Services.Steam;

namespace Moodex.ViewModels.Settings
{
    public class SettingsWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SettingsSection> Sections { get; }

        private SettingsSection _selectedSection;
        public SettingsSection SelectedSection
        {
            get => _selectedSection;
            set
            {
                if (_selectedSection != value)
                {
                    _selectedSection = value;
                    OnPropertyChanged(nameof(SelectedSection));
                }
            }
        }

        public SettingsWindowViewModel(ISettingsService settingsService, IDialogService dialogService,
            ISteamDetectionService steamDetection, ISteamImportService steamImport, ISteamWebApiService steamWebApi,
            IIgdbService igdbService, MoodexState moodexState)
        {
            Sections = new ObservableCollection<SettingsSection>
            {
              new SettingsSection("General",   new GeneralSettingsViewModel(settingsService)),
              new SettingsSection("Input",  new InputSettingsViewModel(settingsService, dialogService)),
              new SettingsSection("Interface", new InterfaceSettingsViewModel(settingsService)),
              new SettingsSection("Library",   new LibrarySettingsViewModel(settingsService, igdbService, moodexState)),
              new SettingsSection("Storage",   new StorageSettingsViewModel(settingsService, dialogService)),
              new SettingsSection("Launchers", new LaunchersSettingsViewModel(settingsService, steamDetection, steamImport, steamWebApi))
            };

            _selectedSection = Sections.First();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public class SettingsSection
    {
        public string Name { get; }
        public object ViewModel { get; }

        public SettingsSection(string name, object viewModel)
        {
            Name = name;
            ViewModel = viewModel;
        }
    }
}

