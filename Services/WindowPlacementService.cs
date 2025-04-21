using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SamsGameLauncher.Services
{
    public class WindowPlacementService : IWindowPlacementService
    {
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        // P/Invoke signatures
        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        public void PlaceProcessWindows(Process process, Screen target, Screen fallback)
        {
            if (process == null) return;

            // wait for the initial UI to appear
            process.WaitForInputIdle(2000);

            // capture any top‑level windows that already exist
            var seenHandles = EnumerateProcessWindows(process.Id).ToHashSet();

            // move them immediately
            MoveHandles(seenHandles, target, fallback);

            // now spin up a background task that will watch for any new ones
            _ = Task.Run(async () =>
            {
                var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                while (DateTime.UtcNow < deadline)
                {
                    await Task.Delay(200);
                    var all = EnumerateProcessWindows(process.Id);
                    var newcomers = all.Where(h => !seenHandles.Contains(h)).ToList();
                    if (newcomers.Count > 0)
                    {
                        MoveHandles(newcomers, target, fallback);
                        foreach (var h in newcomers) seenHandles.Add(h);
                    }
                }
            });
        }

        // get all visible, top‑level windows for a given PID
        private static IEnumerable<IntPtr> EnumerateProcessWindows(int pid)
        {
            var results = new List<IntPtr>();
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out var owner);
                if (owner == pid && IsWindowVisible(hWnd))
                    results.Add(hWnd);
                return true;
            }, IntPtr.Zero);
            return results;
        }

        // move each handle to cover the target monitor
        private static void MoveHandles(IEnumerable<IntPtr> handles, Screen target, Screen fallback)
        {
            if (!handles.Any())
            {
                // if we found *no* windows, fall back
                target = fallback;
            }

            var r = target.Bounds;
            uint flags = SWP_NOZORDER | SWP_NOACTIVATE;

            foreach (var h in handles)
            {
                SetWindowPos(h, IntPtr.Zero,
                    r.X, r.Y,
                    r.Width, r.Height,
                    flags);
            }
        }
    }
}
