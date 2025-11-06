using System;
using Moodex.Utilities;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Moodex.Models
{
    public class GameInfo : INotifyPropertyChanged
    {
        // Game data loaded/saved to games.json
        public string Name { get; set; } = string.Empty;
        public string ConsoleId { get; set; } = string.Empty;
        public string FileSystemPath { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; } = DateTime.MinValue;
        
        public bool HasAutoHotKeyScript { get; set; } = false;
        public ObservableCollection<InputScriptInfo> InputScripts { get; } = new ObservableCollection<InputScriptInfo>();
        public bool HasScripts => InputScripts.Count > 0;

        // Per-game controller settings
        public bool ControllerEnabled { get; set; }
        public bool ControllerProfileConfigured { get; set; }

        // Runtime-only helpers
        public string? ConsoleName => Utilities.ConsoleRegistry.GetDisplayName(ConsoleId);
        public string? GameCoverUri => GameCoverLocator.FindGameCover(this);
        public int ReleaseYear => ReleaseDate.Year;
        public bool IsInArchive { get; set; }

        // New runtime fields for manifest-driven layout
        public string? GameRootPath { get; set; }
        public string? GameGuid { get; set; }
        public string? LaunchTarget { get; set; }

        // Completion flags
        private bool _completedAnyPercent;
        public bool CompletedAnyPercent
        {
            get => _completedAnyPercent;
            set
            {
                if (_completedAnyPercent != value)
                {
                    _completedAnyPercent = value;
                    OnPropertyChanged(nameof(CompletedAnyPercent));
                    OnPropertyChanged(nameof(CompletionGroupName));
                    OnPropertyChanged(nameof(CompletionGroupOrder));
                }
            }
        }
        private bool _completedMaxDifficulty;
        public bool CompletedMaxDifficulty
        {
            get => _completedMaxDifficulty;
            set
            {
                if (_completedMaxDifficulty != value)
                {
                    _completedMaxDifficulty = value;
                    OnPropertyChanged(nameof(CompletedMaxDifficulty));
                    OnPropertyChanged(nameof(CompletionGroupName));
                    OnPropertyChanged(nameof(CompletionGroupOrder));
                }
            }
        }
        private bool _completedHundredPercent;
        public bool CompletedHundredPercent
        {
            get => _completedHundredPercent;
            set
            {
                if (_completedHundredPercent != value)
                {
                    _completedHundredPercent = value;
                    OnPropertyChanged(nameof(CompletedHundredPercent));
                    OnPropertyChanged(nameof(CompletionGroupName));
                    OnPropertyChanged(nameof(CompletionGroupOrder));
                }
            }
        }
        public string CompletionGroupName
            => CompletedHundredPercent ? "100% Complete"
             : CompletedMaxDifficulty ? "Max Difficulty"
             : CompletedAnyPercent ? "Any%"
             : "Not Completed";
        public int CompletionGroupOrder
            => CompletedHundredPercent ? 0
             : CompletedMaxDifficulty ? 1
             : CompletedAnyPercent ? 2
             : 3;

        // Processing overlay support
        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;
            set { if (_isProcessing != value) { _isProcessing = value; OnPropertyChanged(nameof(IsProcessing)); } }
        }

        private double _processingPercent;
        public double ProcessingPercent
        {
            get => _processingPercent;
            set { if (Math.Abs(_processingPercent - value) > double.Epsilon) { _processingPercent = value; OnPropertyChanged(nameof(ProcessingPercent)); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
