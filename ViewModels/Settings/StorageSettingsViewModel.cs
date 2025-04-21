using SamsGameLauncher.Configuration;
using SamsGameLauncher.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SamsGameLauncher.ViewModels.Settings
{
    public class StorageSettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _svc;
        private readonly SettingsModel _model;

        // “Active” or “Archive” — popu­lates the two radio buttons
        public ObservableCollection<LibraryKind> Libraries { get; }
            = new ObservableCollection<LibraryKind>(
                  Enum.GetValues<LibraryKind>());

        private LibraryKind _primary;
        public LibraryKind PrimaryLibrary
        {
            get => _primary;
            set
            {
                if (_primary != value)
                {
                    _primary = value;
                    _model.PrimaryLibrary = value;
                    _svc.Save(_model);
                    OnPropertyChanged();
                }
            }
        }

        private string _activePath = "";
        public string ActiveLibraryPath
        {
            get => _activePath;
            set
            {
                if (_activePath != value)
                {
                    _activePath = value;
                    _model.ActiveLibraryPath = value;
                    _svc.Save(_model);
                    OnPropertyChanged();
                }
            }
        }

        private string _archivePath = "";
        public string ArchiveLibraryPath
        {
            get => _archivePath;
            set
            {
                if (_archivePath != value)
                {
                    _archivePath = value;
                    _model.ArchiveLibraryPath = value;
                    _svc.Save(_model);
                    OnPropertyChanged();
                }
            }
        }

        public StorageSettingsViewModel(ISettingsService svc)
        {
            _svc = svc;
            _model = _svc.Load();

            // seed properties from JSON
            PrimaryLibrary = _model.PrimaryLibrary;
            ActiveLibraryPath = _model.ActiveLibraryPath;
            ArchiveLibraryPath = _model.ArchiveLibraryPath;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
