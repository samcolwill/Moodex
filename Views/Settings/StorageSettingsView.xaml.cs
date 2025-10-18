using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace SamsGameLauncher.Views.Settings
{
    public partial class StorageSettingsView : System.Windows.Controls.UserControl
    {
        public StorageSettingsView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }
}