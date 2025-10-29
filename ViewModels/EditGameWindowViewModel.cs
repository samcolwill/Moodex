using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Moodex.Models;
using Moodex.Services;

namespace Moodex.ViewModels
{
    public class EditGameWindowViewModel : BaseViewModel
    {
        private readonly GameInfo _originalGame;

        // ───────────── backing fields ─────────────
        private string _name = "";
        private string _fileSystemPath = "";
        private string _consoleId = "";
        private string _genre = "";
        private DateTime _releaseDate = DateTime.Today;

        // which console-IDs should show the “folder picker”?
        private static readonly HashSet<string> _folderConsoleIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "Playstation3"
        };

        private bool IsFolderBasedConsole => !string.IsNullOrEmpty(ConsoleId)
            && _folderConsoleIds.Contains(ConsoleId);

        // ───────────── dropdown sources ─────────────
        public IReadOnlyList<ConsoleInfo> Consoles { get; }
        public IReadOnlyList<string> Genres { get; }

        // ───────────── bindable props ─────────────
        public string Name
        {
            get => _name;
            set { _name = value; RaisePropertyChanged(); SaveCommand.NotifyCanExecuteChanged(); }
        }

        public string FileSystemPath
        {
            get => _fileSystemPath;
            set { _fileSystemPath = value; RaisePropertyChanged(); SaveCommand.NotifyCanExecuteChanged(); }
        }

        public string ConsoleId
        {
            get => _consoleId;
            set { _consoleId = value; RaisePropertyChanged(); }
        }

        public string Genre
        {
            get => _genre;
            set { _genre = value; RaisePropertyChanged(); }
        }

        public DateTime ReleaseDate
        {
            get => _releaseDate;
            set { _releaseDate = value; RaisePropertyChanged(); }
        }

        // ───────────── commands (Toolkit) ─────────────
        public IRelayCommand BrowseCommand { get; }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }

        // ───────────── ctor ─────────────
        [SupportedOSPlatform("windows")]
        public EditGameWindowViewModel(GameInfo gameToEdit,
                                       ISettingsService settingsService)
        {
            _originalGame = gameToEdit;

            var settings = settingsService.Load();
            Consoles = settings.Consoles;
            Genres = settings.Genres;

            // wire up commands
            BrowseCommand = new RelayCommand<Window?>(ExecuteBrowse);
            SaveCommand = new RelayCommand<Window?>(ExecuteSave, _ => CanSave());
            CancelCommand = new RelayCommand<Window?>(w => w?.Close());

            // seed the form
            Name = gameToEdit.Name;
            FileSystemPath = gameToEdit.FileSystemPath;
            ConsoleId = gameToEdit.ConsoleId;
            Genre = gameToEdit.Genre;
            ReleaseDate = gameToEdit.ReleaseDate;
        }

        // ───────────── command bodies ─────────────
        [SupportedOSPlatform("windows")]
        private void ExecuteBrowse(Window? owner)
        {
            if (IsFolderBasedConsole)
            {
                var fb = new System.Windows.Forms.FolderBrowserDialog();
                if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    FileSystemPath = fb.SelectedPath;
            }
            else
            {
                var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "All Files (*.*)|*.*" };
                if (ofd.ShowDialog(owner) == true)
                    FileSystemPath = ofd.FileName;
            }
        }

        private bool CanSave()
            => !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(FileSystemPath)
            && !string.IsNullOrWhiteSpace(ConsoleId);

        private void ExecuteSave(Window? owner)
        {
            // copy back into the model
            _originalGame.Name = Name;
            _originalGame.FileSystemPath = FileSystemPath;
            _originalGame.ConsoleId = ConsoleId;
            _originalGame.Genre = Genre;
            _originalGame.ReleaseDate = ReleaseDate;

            if (owner != null)
            {
                owner.DialogResult = true;
                owner.Close();
            }
        }
    }
}

