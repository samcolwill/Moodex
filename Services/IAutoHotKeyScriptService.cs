using Moodex.Models;

namespace Moodex.Services
{
    public interface IAutoHotKeyScriptService
    {
        /// <summary>
        /// Creates a new AutoHotKey script for a game
        /// </summary>
        /// <param name="game">The game to create a script for</param>
        /// <returns>The path to the created script file</returns>
        string CreateScript(GameInfo game);

        /// <summary>
        /// Opens an existing script for editing
        /// </summary>
        /// <param name="game">The game with the script to edit</param>
        void EditScript(GameInfo game);
        void EditScript(GameInfo game, string scriptName);

        /// <summary>
        /// Deletes a script for a game
        /// </summary>
        /// <param name="game">The game to delete the script for</param>
        void DeleteScript(GameInfo game);
        void DeleteScript(GameInfo game, string scriptName);

        /// <summary>
        /// Launches a script for a game if it exists
        /// </summary>
        /// <param name="game">The game to launch the script for</param>
        void LaunchScript(GameInfo game);

        /// <summary>
        /// Checks if a game has a script
        /// </summary>
        /// <param name="game">The game to check</param>
        /// <returns>True if the game has a script</returns>
        bool HasScript(GameInfo game);

        /// <summary>
        /// Enables or disables a named script for a game and persists to manifest
        /// </summary>
        void SetScriptEnabled(GameInfo game, string scriptName, bool enabled);

        /// <summary>
        /// Gets the .ahk script path for a game
        /// </summary>
        /// <param name="game">The game to get the script path for</param>
        /// <returns>Path to the .ahk script file, or null if the game's main folder cannot be determined</returns>
        string? GetAhkScriptPath(GameInfo game);
    }
}


