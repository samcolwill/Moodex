using System.IO;

namespace SamsGameLauncher.Utilities
{
    public static class GameCoverLocator
    {
        // Returns the first matching image file path, or null if none found
        public static string? FindGameCover(string directory, string baseFileName)
        {
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(baseFileName))
                return null;

            foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
            {
                var candidate = Path.Combine(directory, baseFileName + ext);
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }
    }
}