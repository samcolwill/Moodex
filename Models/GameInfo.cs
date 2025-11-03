using System;
using Moodex.Utilities;
using System.ComponentModel;

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

        // Runtime-only helpers
        public string? ConsoleName => Utilities.ConsoleRegistry.GetDisplayName(ConsoleId);
        public string? GameCoverUri => GameCoverLocator.FindGameCover(this);
        public int ReleaseYear => ReleaseDate.Year;
        public bool IsInArchive { get; set; }

        // New runtime fields for manifest-driven layout
        public string? GameRootPath { get; set; }
        public string? GameGuid { get; set; }
        public string? LaunchTarget { get; set; }

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
