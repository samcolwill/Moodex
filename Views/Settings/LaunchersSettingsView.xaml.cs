using Moodex.ViewModels.Settings;

namespace Moodex.Views.Settings
{
    public partial class LaunchersSettingsView : System.Windows.Controls.UserControl
    {
        public LaunchersSettingsView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LaunchersSettingsViewModel vm && !string.IsNullOrEmpty(vm.SteamApiKey))
                ApiKeyBox.Password = vm.SteamApiKey;
        }

        private void ApiKeyBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is LaunchersSettingsViewModel vm)
                vm.SteamApiKey = ApiKeyBox.Password;
        }
    }
}
