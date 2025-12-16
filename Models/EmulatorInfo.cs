using Moodex.Utilities;
using System.Collections.Generic;
using System.Linq;
using System;
using System.ComponentModel;

namespace Moodex.Models
{
    public class EmulatorInfo : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string? _guid;
        private List<string> _emulatedConsoleIds = new List<string>();
        private string _executablePath = string.Empty;
        private string _defaultArguments = string.Empty;

        // Emulator data loaded/saved to emulators.json
        public string Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(nameof(Id)); OnPropertyChanged(nameof(EmulatedConsoleNames)); } }
        }
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
        }
        public string? Guid
        {
            get => _guid;
            set { if (_guid != value) { _guid = value; OnPropertyChanged(nameof(Guid)); } }
        }
        public List<string> EmulatedConsoleIds
        {
            get => _emulatedConsoleIds;
            set { if (!ReferenceEquals(_emulatedConsoleIds, value)) { _emulatedConsoleIds = value ?? new List<string>(); OnPropertyChanged(nameof(EmulatedConsoleIds)); OnPropertyChanged(nameof(EmulatedConsoleNames)); } }
        }
        public string ExecutablePath
        {
            get => _executablePath;
            set { if (_executablePath != value) { _executablePath = value; OnPropertyChanged(nameof(ExecutablePath)); } }
        }
        public string DefaultArguments
        {
            get => _defaultArguments;
            set { if (_defaultArguments != value) { _defaultArguments = value; OnPropertyChanged(nameof(DefaultArguments)); } }
        }

        // Runtime-only helpers
        public string? EmulatedConsoleNames
            => EmulatedConsoleIds == null || EmulatedConsoleIds.Count == 0
               ? null
               : string.Join(", ",
                   EmulatedConsoleIds
                       .Select(ConsoleRegistry.GetDisplayName)
                       .Where(n => !string.IsNullOrWhiteSpace(n)));

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
