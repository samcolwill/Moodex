using SamsGameLauncher.Configuration;

namespace SamsGameLauncher.Services
{
    public interface ISettingsService
    {
        SettingsModel Load();
        void Save(SettingsModel model);
    }
}