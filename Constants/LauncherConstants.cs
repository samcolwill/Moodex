namespace SamsGameLauncher.Constants
{
    public static class LauncherConstants
    {
        // Available console platforms
        public static readonly IReadOnlyList<string> Consoles = new[]
        {
            "PC",
            "PlayStation 1",
            "PlayStation 2",
            "PlayStation 3",
            "Game Boy",
            "Game Boy Color",
            "Game Boy Advance",
            "Nintendo Switch"
        };

        // Supported game genres
        public static readonly IReadOnlyList<string> Genres = new[]
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
