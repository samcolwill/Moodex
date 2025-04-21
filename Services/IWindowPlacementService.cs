using System.Diagnostics;
using System.Windows.Forms;

namespace SamsGameLauncher.Services
{
    public interface IWindowPlacementService
    {
        void PlaceProcessWindows(Process process, Screen target, Screen fallback);
    }
}