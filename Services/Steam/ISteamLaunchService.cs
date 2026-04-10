using Moodex.Models;

namespace Moodex.Services.Steam
{
    public interface ISteamLaunchService
    {
        void LaunchSteamGame(GameInfo game);
        void InstallSteamGame(int appId);
        void UninstallSteamGame(int appId);
    }
}
