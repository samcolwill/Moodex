using System.Collections.ObjectModel;
using Moodex.Models;

namespace Moodex.Services
{
    public class MoodexState
    {
        public ObservableCollection<EmulatorInfo> Emulators { get; set; } = new();
        public ObservableCollection<GameInfo> Games { get; set; } = new();
    }
}


