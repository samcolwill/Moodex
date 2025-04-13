using System.Collections.Generic;

namespace SamsGameLauncher.Constants
{
    public static class LauncherConstants
    {
        public static List<string> Consoles { get; } = new List<string>
        {
            "PC",
            "PlayStation 1",
            "PlayStation 2",
            "PlayStation 3",
            "Game Boy",
            "Game Boy Color",
            "Game Boy Advance"
        };

        public static List<string> Genres { get; } = new List<string>
        {
            "Action",
            "Adventure",
            "Fighting",
            "Platform",
            "Puzzle",
            "Racing",
            "Role-playing",
            "Shooter",
            "Simulation",
            "Sports",
            "Strategy"
        };
    }
}
