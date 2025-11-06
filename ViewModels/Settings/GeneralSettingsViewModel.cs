using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Input;
using Moodex.Configuration;
using Moodex.Services;

namespace Moodex.ViewModels.Settings
{
    public class GeneralSettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;
        private readonly SettingsModel _model;

        public ObservableCollection<string> GroupByOptions { get; } = new ObservableCollection<string>
        {
            "Console", "Completion", "Genre", "Year"
        };

        private string _selectedDefaultGroupBy = "Console";
        public string SelectedDefaultGroupBy
        {
            get => _selectedDefaultGroupBy;
            set
            {
                if (_selectedDefaultGroupBy == value) return;
                _selectedDefaultGroupBy = value;
                _model.DefaultGroupBy = value;
                _settingsService.Save(_model);
                RaisePropertyChanged();
            }
        }

        public IRelayCommand SaveCommand { get; }

        public GeneralSettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _model = _settingsService.Load();
            _selectedDefaultGroupBy = string.IsNullOrWhiteSpace(_model.DefaultGroupBy) ? "Console" : _model.DefaultGroupBy;
            SaveCommand = new RelayCommand(() => _settingsService.Save(_model));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string p = null!)
          => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

}
