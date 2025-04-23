using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SamsGameLauncher.Configuration;
using SamsGameLauncher.Models;
using SamsGameLauncher.Services;

namespace SamsGameLauncher.ViewModels.Settings
{
    public class InterfaceSettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;
        private readonly SettingsModel _model;

        private const string NoMonitorPlaceholder = "[None]";

        public record MonitorInfo(string Name, string DeviceName, int Index)
        {
            // in case WPF or anything calls ToString()
            public override string ToString() => Name;
        }

        public ObservableCollection<MonitorInfo> Monitors { get; }
        public ObservableCollection<string> LaunchModes { get; }

        private MonitorInfo _selectedMonitor;
        public MonitorInfo SelectedMonitor
        {
            get => _selectedMonitor;
            set
            {
                if (SetField(ref _selectedMonitor, value))
                {
                    _model.DefaultMonitorIndex = value.Index;
                    _settingsService.Save(_model);
                }
            }
        }

        private string _selectedLaunchMode = string.Empty;
        public string SelectedLaunchMode
        {
            get => _selectedLaunchMode;
            set
            {
                if (SetField(ref _selectedLaunchMode, value))
                {
                    _model.LaunchMode = value;
                    _settingsService.Save(_model);
                }
            }
        }

        private int _resolutionWidth;
        public int ResolutionWidth
        {
            get => _resolutionWidth;
            set
            {
                if (SetField(ref _resolutionWidth, value))
                {
                    _model.ResolutionWidth = value;
                    _settingsService.Save(_model);
                }
            }
        }

        private int _resolutionHeight;
        public int ResolutionHeight
        {
            get => _resolutionHeight;
            set
            {
                if (SetField(ref _resolutionHeight, value))
                {
                    _model.ResolutionHeight = value;
                    _settingsService.Save(_model);
                }
            }
        }

        public bool IsResolutionEnabled =>
            !string.Equals(SelectedLaunchMode, "Fullscreen", StringComparison.OrdinalIgnoreCase);

        public InterfaceSettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _model = _settingsService.Load();

            // build our dropdown list in the exact same order as Screen.AllScreens
            var list = new List<MonitorInfo>
            {
              new MonitorInfo(NoMonitorPlaceholder, "", -1)
            };

            var screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                var scr = screens[i];

                // 1) P/Invoke to get the hardware ID for this display
                var dd = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
                EnumDisplayDevices(scr.DeviceName, 0, ref dd, 0);
                // DeviceID looks like "DISPLAY\\HEC0030\\7&…"
                var parts = dd.DeviceID.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                var hwId = parts.Length > 1 ? parts[1] : "";

                // 2) Try WMI by *hardware* ID
                var friendly = GetWmiMonitorName(hwId)
                                // 3) then fall back to the EDID name
                                ?? dd.DeviceString.Trim()
                                // 4) then last‑resort raw
                                ?? scr.DeviceName;

                list.Add(new MonitorInfo(friendly, scr.DeviceName, i));
            }

            Monitors = new ObservableCollection<MonitorInfo>(list);

            // select by saved index (or default to first)
            int idx = _model.DefaultMonitorIndex;
            SelectedMonitor = Monitors.FirstOrDefault(m => m.Index == idx)
                             ?? Monitors[0];

            // launch modes & other seeds
            LaunchModes = new ObservableCollection<string> { "Fullscreen", "Windowed", "Borderless" };
            SelectedLaunchMode = _model.LaunchMode;
            ResolutionWidth = _model.ResolutionWidth;
            ResolutionHeight = _model.ResolutionHeight;
        }

        // Gets exactly the PnP-registered Name from Win32_PnPEntity (Device Manager)
        private static string? GetWmiMonitorName(string hardwareId)
        {
            if (string.IsNullOrEmpty(hardwareId))
                return null;

            try
            {
                // Only monitors
                var searcher = new ManagementObjectSearcher(
                  "root\\CIMV2",
                  "SELECT Name, DeviceID FROM Win32_PnPEntity WHERE PNPClass='Monitor'");

                foreach (ManagementObject mo in searcher.Get())
                {
                    var devId = (mo["DeviceID"] as string) ?? "";
                    var name = (mo["Name"] as string) ?? "";

                    // match the hardware ID itself, e.g. "HEC0030" or "ACI24AA"
                    if (devId.Contains(hardwareId, StringComparison.OrdinalIgnoreCase))
                        return name.Trim();
                }
            }
            catch
            {
                // swallow WMI errors
            }
            return null;
        }

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        static extern bool EnumDisplayDevices(
            string? lpDevice, uint iDevNum,
            ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct DISPLAY_DEVICE
        {
            public int cb;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;

            public DisplayDeviceStateFlags StateFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [Flags]
        enum DisplayDeviceStateFlags : int
        {
            AttachedToDesktop = 0x00000001,
            PrimaryDevice = 0x00000004
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool SetField<T>(ref T field, T value, [CallerMemberName] string n = "")
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
            return true;
        }
    }
}
