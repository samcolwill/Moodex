using System.IO;
using SamsGameLauncher.Models;

namespace SamsGameLauncher.Utilities
{
    public static class GameCoverLocator
    {
        public static string? FindGameCover(GameInfo game)
        {
            if (game == null) return null;

            // 1) figure out the folder: if it's a file use its parent dir, if it's a dir use it
            var path = game.FileSystemPath;
            if (string.IsNullOrWhiteSpace(path)) return null;

            string folder = File.Exists(path)
                ? Path.GetDirectoryName(path)!
                : path;

            if (!Directory.Exists(folder)) return null;

            // 2) look for Name.png/jpg/jpeg
            foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
            {
                var candidate = Path.Combine(folder, game.Name + ext);
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }
    }
}