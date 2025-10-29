using Moodex.Configuration;

namespace Moodex.Services
{
    public interface ISettingsService
    {
        SettingsModel Load();
        void Save(SettingsModel model);
    }
}
