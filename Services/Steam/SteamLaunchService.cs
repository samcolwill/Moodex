using System.Diagnostics;
using Moodex.Models;

namespace Moodex.Services.Steam
{
    public class SteamLaunchService : ISteamLaunchService
    {
        private readonly ISettingsService _settings;

        public SteamLaunchService(ISettingsService settings)
        {
            _settings = settings;
        }

        public void LaunchSteamGame(GameInfo game)
        {
            if (game.SteamAppId == null) return;

            EnsureSteamRunning();

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"steam://rungameid/{game.SteamAppId}",
                    UseShellExecute = true
                });
            }
            catch
            {
                LaunchViaExe($"-applaunch {game.SteamAppId}");
            }
        }

        public void InstallSteamGame(int appId)
        {
            EnsureSteamRunning();

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"steam://install/{appId}",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        public void UninstallSteamGame(int appId)
        {
            EnsureSteamRunning();

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"steam://uninstall/{appId}",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void EnsureSteamRunning()
        {
            if (Process.GetProcessesByName("steam").Length > 0)
                return;

            var steamSettings = _settings.Load().Steam;
            if (string.IsNullOrEmpty(steamSettings.SteamExePath))
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = steamSettings.SteamExePath,
                    UseShellExecute = true
                });
                Thread.Sleep(3000);
            }
            catch { }
        }

        private void LaunchViaExe(string arguments)
        {
            var steamSettings = _settings.Load().Steam;
            if (string.IsNullOrEmpty(steamSettings.SteamExePath)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = steamSettings.SteamExePath,
                    Arguments = arguments,
                    UseShellExecute = false
                });
            }
            catch { }
        }
    }
}
