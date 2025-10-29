using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Moodex.Services;

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

        // DI will inject your ISettingsService here
        public SettingsWindowViewModel(ISettingsService settingsService, IDialogService dialogService)
        {
            Sections = new ObservableCollection<SettingsSection>
            {
              new SettingsSection("General",   new GeneralSettingsViewModel(settingsService)),
              new SettingsSection("Interface", new InterfaceSettingsViewModel(settingsService)),
              new SettingsSection("Library",   new LibrarySettingsViewModel(settingsService)),
              new SettingsSection("Storage",   new StorageSettingsViewModel(settingsService, dialogService)),
              new SettingsSection("Controller",  new ControllerSettingsViewModel(settingsService, dialogService))
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

