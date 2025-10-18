using CommunityToolkit.Mvvm.Input;
using SamsGameLauncher.Configuration;
using SamsGameLauncher.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace SamsGameLauncher.ViewModels.Settings
{
    public class StorageSettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _svc;
        private readonly SettingsModel _model;
        private readonly IDialogService _dialog;

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

        public StorageSettingsViewModel(ISettingsService svc, IDialogService dialog)
        {
            _svc = svc;
            _model = _svc.Load();
            _dialog = dialog;

            // seed properties from JSON
            PrimaryLibrary = _model.PrimaryLibrary;
            ActiveLibraryPath = _model.ActiveLibraryPath;
            ArchiveLibraryPath = _model.ArchiveLibraryPath;
            _sevenZipPath = _model.SevenZipPath;

            // Commands
            BrowseFor7zipCommand = new RelayCommand(BrowseFor7zip);
        }

        // ——— 7zip Properties —————————————————————————————————————————
        private string _sevenZipPath;
        public string SevenZipPath
        {
            get => _sevenZipPath;
            set
            {
                if (_sevenZipPath != value)
                {
                    _sevenZipPath = value;
                    _model.SevenZipPath = value;
                    _svc.Save(_model);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Is7zipConfigured));
                }
            }
        }

        public bool Is7zipConfigured => !string.IsNullOrEmpty(SevenZipPath) && File.Exists(SevenZipPath);

        // ——— Commands —————————————————————————————————————————
        public IRelayCommand BrowseFor7zipCommand { get; }

        // ——— 7zip Command Handlers ——————————————————————————————

        private void BrowseFor7zip()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select 7zip Executable",
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                InitialDirectory = @"C:\Program Files\7-Zip"
            };

            if (dialog.ShowDialog() == true)
            {
                SevenZipPath = dialog.FileName;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
