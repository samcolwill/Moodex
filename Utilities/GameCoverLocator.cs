using System.IO;

namespace SamsGameLauncher.Utilities
{
    public static class GameCoverLocator
    {
        // Checks for an image file with the provided base name in the specified directory.
        public static string FindGameCover(string directory, string baseFileName)
        {
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(baseFileName))
                return null;

            string[] extensions = new[] { ".png", ".jpg", ".jpeg" };
            foreach (var ext in extensions)
            {
                var candidate = Path.Combine(directory, baseFileName + ext);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            return null;
        }
    }
}
