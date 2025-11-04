using System.Windows.Controls;
using System.Windows.Navigation;

namespace Moodex.Views.Settings
{
    public partial class InputSettingsView : System.Windows.Controls.UserControl
    {
        public InputSettingsView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}


