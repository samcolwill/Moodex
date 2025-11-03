using System.IO;
using Moodex.Models;

namespace Moodex.Utilities
{
    public static class GameCoverLocator
    {
        public static string? FindGameCover(GameInfo game)
        {
            if (game == null) return null;

            // Prefer explicit cover.* in the game root when available
            var root = game.GameRootPath;
            if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
            {
                foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
                {
                    var c = Path.Combine(root, "cover" + ext);
                    if (File.Exists(c)) return c;
                }
            }

            // Fallback: old heuristic next to the file using game name
            var path = game.FileSystemPath;
            if (string.IsNullOrWhiteSpace(path)) return null;
            var folder = File.Exists(path) ? Path.GetDirectoryName(path)! : path;
            if (!Directory.Exists(folder)) return null;
            foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
            {
                var candidate = Path.Combine(folder, game.Name + ext);
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }
    }
}
