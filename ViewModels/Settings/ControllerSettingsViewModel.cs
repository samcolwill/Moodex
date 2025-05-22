using SamsGameLauncher.Configuration;
using SamsGameLauncher.Services;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Diagnostics;

namespace SamsGameLauncher.ViewModels.Settings
{
    public class ControllerSettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _svc;
        private readonly SettingsModel _model;

        public ControllerSettingsViewModel(ISettingsService svc)
        {
            _svc = svc;
            _model = _svc.Load();

            // Initialize from persisted model
            _launchDs4WindowsOnStartup = _model.LaunchDs4WindowsOnStartup;
            _isDs4Installed = _model.IsDs4Installed;

            // Commands
            DownloadInstallDs4Command = new AsyncRelayCommand(DownloadAndInstallDs4Async, () => CanInstallDs4);
            LaunchDs4WindowsCommand = new RelayCommand(LaunchDs4Windows, () => IsDs4Installed);
            UninstallDs4WindowsCommand = new RelayCommand(UninstallDs4Windows, () => IsDs4Installed);
        }

        // ——— Properties —————————————————————————————————————————

        private bool _launchDs4WindowsOnStartup;
        public bool LaunchDs4WindowsOnStartup
        {
            get => _launchDs4WindowsOnStartup;
            set
            {
                if (_launchDs4WindowsOnStartup != value)
                {
                    _launchDs4WindowsOnStartup = value;
                    _model.LaunchDs4WindowsOnStartup = value;
                    _svc.Save(_model);
                    OnPropertyChanged();
                }
            }
        }

        private bool _isDs4Installed;
        public bool IsDs4Installed
        {
            get => _isDs4Installed;
            private set
            {
                if (_isDs4Installed != value)
                {
                    _isDs4Installed = value;
                    _model.IsDs4Installed = value;
                    _svc.Save(_model);
                    OnPropertyChanged();

                    // Refresh command enabled/disabled states
                    DownloadInstallDs4Command.NotifyCanExecuteChanged();
                    LaunchDs4WindowsCommand.NotifyCanExecuteChanged();
                    UninstallDs4WindowsCommand.NotifyCanExecuteChanged();

                    OnPropertyChanged(nameof(CanInstallDs4));
                }
            }
        }

        public bool CanInstallDs4 => !IsDs4Installed;

        // ——— Commands —————————————————————————————————————————

        public IAsyncRelayCommand DownloadInstallDs4Command { get; }
        public IRelayCommand LaunchDs4WindowsCommand { get; }
        public Process? LastLaunchedProcess { get; private set; }
        public IRelayCommand UninstallDs4WindowsCommand { get; }

        // ——— Command Handlers ——————————————————————————————————

        private const string GitHubLatestReleaseUrl = "https://api.github.com/repos/Ryochan7/DS4Windows/releases/latest";
        private async Task DownloadAndInstallDs4Async()
        {
            // 1) Prepare install folder
            var exeDir = AppContext.BaseDirectory;
            var targetDir = Path.Combine(exeDir, "DS4Windows");

            if (Directory.Exists(targetDir))
                Directory.Delete(targetDir, recursive: true);
            Directory.CreateDirectory(targetDir);

            // 2) Fetch release metadata
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("SamsGameLauncher");
            var json = await http.GetStringAsync(GitHubLatestReleaseUrl);
            using var doc = JsonDocument.Parse(json);
            var assets = doc.RootElement.GetProperty("assets").EnumerateArray();

            string? zipUrl = null;
            foreach (var asset in assets)
            {
                var name = asset.GetProperty("name").GetString();
                if (name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true)
                {
                    zipUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }
            if (zipUrl == null)
                throw new InvalidOperationException("No ZIP asset found in DS4Windows release.");

            // 3) Download ZIP to temp file
            var tmpFile = Path.GetTempFileName();
            using (var resp = await http.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                resp.EnsureSuccessStatusCode();
                await using var fs = File.Create(tmpFile);
                await resp.Content.CopyToAsync(fs);
            }

            // 4a) Extract
            ZipFile.ExtractToDirectory(tmpFile, targetDir);

            // 4b) Flatten out the extra folder level, if present:
            var installDir = new DirectoryInfo(targetDir);
            var subdirs = installDir.GetDirectories();
            var files = installDir.GetFiles();

            // If there's exactly one subdir and no other files, assume it's the extra nesting:
            if (subdirs.Length == 1 && files.Length == 0)
            {
                var nested = subdirs[0];

                // Move every file up
                foreach (var f in nested.GetFiles())
                {
                    f.MoveTo(Path.Combine(targetDir, f.Name));
                }

                // Move every folder up
                foreach (var d in nested.GetDirectories())
                {
                    d.MoveTo(Path.Combine(targetDir, d.Name));
                }

                // Delete the now-empty folder
                nested.Delete(recursive: true);
            }

            // 5a) Seed Profiles.xml with your default profile data
            var profilesPath = Path.Combine(targetDir, "Profiles.xml");
            const string profilesXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<!-- Profile Configuration Data. 01/01/2025 00:00:00 -->
<!-- Made with DS4Windows version 3.3.3 -->
<Profile app_version=""3.3.3"" config_version=""2"">
  <useExclusiveMode>True</useExclusiveMode>
  <startMinimized>True</startMinimized>
  <minimizeToTaskbar>False</minimizeToTaskbar>
  <formWidth>782</formWidth>
  <formHeight>550</formHeight>
  <formLocationX>0</formLocationX>
  <formLocationY>0</formLocationY>
  <LastChecked>01/01/0001 00:00:00</LastChecked>
  <CheckWhen>24</CheckWhen>
  <Notifications>2</Notifications>
  <DisconnectBTAtStop>False</DisconnectBTAtStop>
  <SwipeProfiles>True</SwipeProfiles>
  <QuickCharge>False</QuickCharge>
  <CloseMinimizes>True</CloseMinimizes>
  <UseLang />
  <DownloadLang>False</DownloadLang>
  <FlashWhenLate>True</FlashWhenLate>
  <FlashWhenLateAt>500</FlashWhenLateAt>
  <AppIcon>Default</AppIcon>
  <AppTheme>Default</AppTheme>
  <UseOSCServer>False</UseOSCServer>
  <OSCServerPort>9000</OSCServerPort>
  <InterpretingOscMonitoring>False</InterpretingOscMonitoring>
  <UseOSCSender>False</UseOSCSender>
  <OSCSenderPort>9001</OSCSenderPort>
  <OSCSenderAddress>127.0.0.1</OSCSenderAddress>
  <UseUDPServer>False</UseUDPServer>
  <UDPServerPort>26760</UDPServerPort>
  <UDPServerListenAddress>127.0.0.1</UDPServerListenAddress>
  <UDPServerSmoothingOptions>
    <UseSmoothing>False</UseSmoothing>
    <UdpSmoothMinCutoff>0.4</UdpSmoothMinCutoff>
    <UdpSmoothBeta>0.2</UdpSmoothBeta>
  </UDPServerSmoothingOptions>
  <UseCustomSteamFolder>False</UseCustomSteamFolder>
  <CustomSteamFolder />
  <AutoProfileRevertDefaultProfile>True</AutoProfileRevertDefaultProfile>
  <AbsRegionDisplay />
  <DeviceOptions>
    <DS4SupportSettings>
      <Enabled>False</Enabled>
    </DS4SupportSettings>
    <DualSenseSupportSettings>
      <Enabled>True</Enabled>
    </DualSenseSupportSettings>
    <SwitchProSupportSettings>
      <Enabled>False</Enabled>
    </SwitchProSupportSettings>
    <JoyConSupportSettings>
      <Enabled>False</Enabled>
      <LinkMode>Joined</LinkMode>
      <JoinedGyroProvider>JoyConL</JoinedGyroProvider>
    </JoyConSupportSettings>
    <DS3SupportSettings>
      <Enabled>False</Enabled>
    </DS3SupportSettings>
  </DeviceOptions>
  <CustomLed1>False:0,0,255</CustomLed1>
  <CustomLed2>False:0,0,255</CustomLed2>
  <CustomLed3>False:0,0,255</CustomLed3>
  <CustomLed4>False:0,0,255</CustomLed4>
  <CustomLed5>False:0,0,255</CustomLed5>
  <CustomLed6>False:0,0,255</CustomLed6>
  <CustomLed7>False:0,0,255</CustomLed7>
  <CustomLed8>False:0,0,255</CustomLed8>
</Profile>";
            File.WriteAllText(profilesPath, profilesXml);

            // 5b) Seed Auto Profiles.xml with your default profile data
            var autoProfilesPath = Path.Combine(targetDir, "Auto Profiles.xml");
            const string autoProfilesXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<!-- Auto-Profile Configuration Data. 01/01/2025 00:00:00 -->
<Programs />";
            File.WriteAllText(autoProfilesPath, autoProfilesXml);

            // 5c) Seed Profiles folder
            var profilesDir = Path.Combine(targetDir, "Profiles");
            if (!Directory.Exists(profilesDir))
                Directory.CreateDirectory(profilesDir);

            // 6) Cleanup + state update
            File.Delete(tmpFile);
            IsDs4Installed = true;
        }

        private void LaunchDs4Windows()
        {
            var exe = Path.Combine(AppContext.BaseDirectory, "DS4Windows", "DS4Windows.exe");
            var proc = Process.Start(new ProcessStartInfo(exe)
            {
                UseShellExecute = true
            });
            LastLaunchedProcess = proc;
        }

        private void UninstallDs4Windows()
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "DS4Windows");

            // 1) Find any running DS4Windows.exe in that folder
            var running = Process.GetProcessesByName("DS4Windows")
                                 .Where(p =>
                                     string.Equals(
                                         Path.GetFullPath(p.MainModule?.FileName ?? ""),
                                         Path.Combine(dir, "DS4Windows.exe"),
                                         StringComparison.OrdinalIgnoreCase))
                                 .ToList();

            // 2) Ask each one to close (politely), then kill if needed
            foreach (var p in running)
            {
                try
                {
                    p.CloseMainWindow();
                    if (!p.WaitForExit(2000))  // 2s to shut down cleanly
                        p.Kill();
                }
                catch
                {
                    // ignore any errors shutting it down
                }
            }

            // 3) Now it’s safe to delete the folder
            if (Directory.Exists(dir))
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (UnauthorizedAccessException)
                {
                    // in the unlikely case something is still locked,
                    // wait a moment and try again once
                    Thread.Sleep(500);
                    Directory.Delete(dir, recursive: true);
                }
            }

            // 4) Reset the flags
            IsDs4Installed = false;
            LaunchDs4WindowsOnStartup = false;
        }

        // ——— INotifyPropertyChanged —————————————————————————————

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}