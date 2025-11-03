using CommunityToolkit.Mvvm.Input;
using Moodex.Configuration;
using Moodex.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Moodex.ViewModels.Settings
{
    public class StorageSettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _svc;
        private readonly SettingsModel _model;
        private readonly IDialogService _dialog;

        public StorageSettingsViewModel(ISettingsService svc, IDialogService dialog)
        {
            _svc = svc;
            _model = _svc.Load();
            _dialog = dialog;

            // seed properties from JSON
            ActiveLibraryPath = _model.ActiveLibraryPath;
            ArchiveLibraryPath = _model.ArchiveLibraryPath;
            _compressOnArchive = _model.CompressOnArchive;

            BrowseLibraryPathCommand = new RelayCommand(BrowseLibraryPath);
            BrowseArchivePathCommand = new RelayCommand(BrowseArchivePath);
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

        private bool _compressOnArchive;
        public bool CompressOnArchive
        {
            get => _compressOnArchive;
            set
            {
                if (_compressOnArchive != value)
                {
                    _compressOnArchive = value;
                    _model.CompressOnArchive = value;
                    _svc.Save(_model);
                    OnPropertyChanged();
                }
            }
        }

        public IRelayCommand BrowseLibraryPathCommand { get; }
        public IRelayCommand BrowseArchivePathCommand { get; }

        private void BrowseLibraryPath()
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var root = System.IO.Path.Combine(dlg.SelectedPath, "Moodex Library");
                ActiveLibraryPath = root;
            }
        }

        private void BrowseArchivePath()
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var root = System.IO.Path.Combine(dlg.SelectedPath, "Moodex Archive");
                ArchiveLibraryPath = root;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}

