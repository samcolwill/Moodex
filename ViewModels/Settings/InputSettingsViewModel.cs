using CommunityToolkit.Mvvm.Input;
using Moodex.Configuration;
using Moodex.Services;
using Moodex.Utilities;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace Moodex.ViewModels.Settings
{
    public class InputSettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _svc;
        private readonly SettingsModel _model;
        private readonly IDialogService _dialog;

        public InputSettingsViewModel(ISettingsService svc, IDialogService dialog)
        {
            _svc = svc;
            _model = _svc.Load();
            _dialog = dialog;

            _launchDs4WindowsOnStartup = _model.LaunchDs4WindowsOnStartup;
            _isDs4Installed = _model.IsDs4Installed;
            _isAutoHotKeyInstalled = _model.IsAutoHotKeyInstalled;

            CheckDs4Installation();
            CheckAutoHotKeyInstallation();

            DownloadInstallDs4Command = new AsyncRelayCommand(DownloadAndInstallDs4Async, () => CanInstallDs4);
            LaunchDs4WindowsCommand = new RelayCommand(LaunchDs4Windows, () => IsDs4Installed);
            UninstallDs4WindowsCommand = new AsyncRelayCommand(ConfirmAndUninstallAsync, () => IsDs4Installed);

            DownloadInstallAutoHotKeyCommand = new AsyncRelayCommand(DownloadAndInstallAutoHotKeyAsync, () => CanInstallAutoHotKey);
            UninstallAutoHotKeyCommand = new AsyncRelayCommand(ConfirmAndUninstallAutoHotKeyAsync, () => IsAutoHotKeyInstalled);
        }

        private bool _launchDs4WindowsOnStartup;
        public bool LaunchDs4WindowsOnStartup
        {
            get => _launchDs4WindowsOnStartup;
            set { if (_launchDs4WindowsOnStartup != value) { _launchDs4WindowsOnStartup = value; _model.LaunchDs4WindowsOnStartup = value; _svc.Save(_model); OnPropertyChanged(); } }
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
                    DownloadInstallDs4Command.NotifyCanExecuteChanged();
                    LaunchDs4WindowsCommand.NotifyCanExecuteChanged();
                    UninstallDs4WindowsCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(CanInstallDs4));
                }
            }
        }
        public bool CanInstallDs4 => !IsDs4Installed;

        private bool _isAutoHotKeyInstalled;
        public bool IsAutoHotKeyInstalled
        {
            get => _isAutoHotKeyInstalled;
            private set
            {
                if (_isAutoHotKeyInstalled != value)
                {
                    _isAutoHotKeyInstalled = value;
                    _model.IsAutoHotKeyInstalled = value;
                    _svc.Save(_model);
                    OnPropertyChanged();
                    DownloadInstallAutoHotKeyCommand.NotifyCanExecuteChanged();
                    UninstallAutoHotKeyCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(CanInstallAutoHotKey));
                }
            }
        }
        public bool CanInstallAutoHotKey => !IsAutoHotKeyInstalled;

        public IAsyncRelayCommand DownloadInstallDs4Command { get; }
        public IRelayCommand LaunchDs4WindowsCommand { get; }
        public Process? LastLaunchedProcess { get; private set; }
        public IRelayCommand UninstallDs4WindowsCommand { get; }

        public IAsyncRelayCommand DownloadInstallAutoHotKeyCommand { get; }
        public IRelayCommand UninstallAutoHotKeyCommand { get; }

        private const string GitHubLatestReleaseUrl = "https://api.github.com/repos/Ryochan7/DS4Windows/releases/latest";
        private async Task DownloadAndInstallDs4Async()
        {
            var exeDir = AppContext.BaseDirectory;
            var targetDir = Path.Combine(exeDir, "External Tools", "DS4Windows");
            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, recursive: true);
            Directory.CreateDirectory(targetDir);
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Moodex");
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
            if (zipUrl == null) throw new InvalidOperationException("No ZIP asset found in DS4Windows release.");
            var tmpFile = Path.GetTempFileName();
            using (var resp = await http.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                resp.EnsureSuccessStatusCode();
                await using var fs = File.Create(tmpFile);
                await resp.Content.CopyToAsync(fs);
            }
            ZipFile.ExtractToDirectory(tmpFile, targetDir);
            File.Delete(tmpFile);
            IsDs4Installed = true;
        }

        private void LaunchDs4Windows()
        {
            var exe = Path.Combine(AppContext.BaseDirectory, "External Tools", "DS4Windows", "DS4Windows.exe");
            var proc = Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
            LastLaunchedProcess = proc;
        }

        private async Task ConfirmAndUninstallAsync()
        {
            bool ok = await _dialog.ShowConfirmationAsync("Confirm DS4Windows Uninstall", "Warning: In addition to uninstalling, this will delete all your DS4Windows configuration and profiles.\n\nAre you sure you want to continue?");
            if (!ok) return;
            UninstallDs4Windows();
        }

        private void UninstallDs4Windows()
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "External Tools", "DS4Windows");
            foreach (var p in Process.GetProcessesByName("DS4Windows"))
            {
                try { p.CloseMainWindow(); if (!p.WaitForExit(2000)) p.Kill(); } catch { }
            }
            if (Directory.Exists(dir)) { try { Directory.Delete(dir, recursive: true); } catch { System.Threading.Thread.Sleep(500); Directory.Delete(dir, recursive: true); } }
            IsDs4Installed = false; LaunchDs4WindowsOnStartup = false;
        }

        private const string AutoHotKeyGitHubLatestReleaseUrl = "https://api.github.com/repos/AutoHotkey/AutoHotkey/releases/latest";
        private async Task DownloadAndInstallAutoHotKeyAsync()
        {
            var exeDir = AppContext.BaseDirectory;
            var targetDir = Path.Combine(exeDir, "External Tools", "AutoHotKey");
            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, recursive: true);
            Directory.CreateDirectory(targetDir);
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Moodex");
            var json = await http.GetStringAsync(AutoHotKeyGitHubLatestReleaseUrl);
            using var doc = JsonDocument.Parse(json);
            var assets = doc.RootElement.GetProperty("assets").EnumerateArray();
            string? zipUrl = null;
            foreach (var asset in assets)
            {
                var name = asset.GetProperty("name").GetString();
                if (name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true && !name.Contains("Source"))
                {
                    zipUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }
            if (zipUrl == null) throw new InvalidOperationException("No ZIP asset found in AutoHotKey release.");
            var tmpFile = Path.GetTempFileName();
            using (var resp = await http.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                resp.EnsureSuccessStatusCode();
                await using var fs = File.Create(tmpFile);
                await resp.Content.CopyToAsync(fs);
            }
            ZipFile.ExtractToDirectory(tmpFile, targetDir);
            File.Delete(tmpFile);
            IsAutoHotKeyInstalled = true;
        }

        private async Task ConfirmAndUninstallAutoHotKeyAsync()
        {
            bool ok = await _dialog.ShowConfirmationAsync("Confirm AutoHotKey Uninstall", "Are you sure you want to uninstall AutoHotKey?");
            if (!ok) return; UninstallAutoHotKey();
        }

        private void UninstallAutoHotKey()
        {
            foreach (var p in Process.GetProcessesByName("AutoHotkey64").Concat(Process.GetProcessesByName("AutoHotkey")).Concat(Process.GetProcessesByName("AutoHotkeyU64")).Concat(Process.GetProcessesByName("AutoHotkeyU32")))
            { try { p.CloseMainWindow(); if (!p.WaitForExit(2000)) p.Kill(); } catch { } }
            var dir = Path.Combine(AppContext.BaseDirectory, "External Tools", "AutoHotKey");
            if (Directory.Exists(dir)) { try { Directory.Delete(dir, recursive: true); } catch { System.Threading.Thread.Sleep(500); Directory.Delete(dir, recursive: true); } }
            IsAutoHotKeyInstalled = false;
        }

        private void CheckDs4Installation()
        { if (File.Exists(Path.Combine(AppContext.BaseDirectory, "External Tools", "DS4Windows", "DS4Windows.exe"))) IsDs4Installed = true; }
        private void CheckAutoHotKeyInstallation()
        { if (File.Exists(Path.Combine(AppContext.BaseDirectory, "External Tools", "AutoHotKey", "AutoHotkey64.exe"))) IsAutoHotKeyInstalled = true; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}


