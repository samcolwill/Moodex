using CommunityToolkit.Mvvm.Input;
using SamsGameLauncher.Configuration;
using SamsGameLauncher.Services;
using SamsGameLauncher.Utilities;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace SamsGameLauncher.ViewModels.Settings
{
    public class ControllerSettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _svc;
        private readonly SettingsModel _model;
        private readonly IDialogService _dialog;

        public ControllerSettingsViewModel(ISettingsService svc, IDialogService dialog)
        {
            _svc = svc;
            _model = _svc.Load();
            _dialog = dialog;

            // Initialize from persisted model
            _launchDs4WindowsOnStartup = _model.LaunchDs4WindowsOnStartup;
            _isDs4Installed = _model.IsDs4Installed;

            // Commands
            DownloadInstallDs4Command = new AsyncRelayCommand(DownloadAndInstallDs4Async, () => CanInstallDs4);
            LaunchDs4WindowsCommand = new RelayCommand(LaunchDs4Windows, () => IsDs4Installed);
            UninstallDs4WindowsCommand = new AsyncRelayCommand(ConfirmAndUninstallAsync, () => IsDs4Installed);
            _dialog = dialog;
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

            // 5) Seed all DS4Windows config files & folders
            var initializer = new DS4WindowsInitializer(targetDir);
            initializer.Initialize();

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

        private async Task ConfirmAndUninstallAsync()
        {
            // 1) ask for confirmation
            bool ok = await _dialog.ShowConfirmationAsync(
                title: "Confirm DS4Windows Uninstall",
                message: "Warning: In addition to uninstalling, this will delete all your DS4Windows configuration and profiles.\n\n" +
                         "Are you sure you want to continue?");
            if (!ok) return;

            // 2) perform the uninstall logic
            UninstallDs4Windows();
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