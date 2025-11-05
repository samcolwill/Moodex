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

        // removed startup launch option; per-game control instead

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

            // Some DS4Windows releases zip a root folder named "DS4Windows"; flatten if present
            try
            {
                var nestedDir = Path.Combine(targetDir, "DS4Windows");
                if (Directory.Exists(nestedDir) && Directory.GetFileSystemEntries(nestedDir).Length > 0)
                {
                    // Move files
                    foreach (var file in Directory.GetFiles(nestedDir))
                    {
                        var dest = Path.Combine(targetDir, Path.GetFileName(file));
                        File.Move(file, dest, overwrite: true);
                    }
                    // Move directories
                    foreach (var dir in Directory.GetDirectories(nestedDir))
                    {
                        var dest = Path.Combine(targetDir, Path.GetFileName(dir));
                        Directory.Move(dir, dest);
                    }
                    Directory.Delete(nestedDir, recursive: true);
                }
            }
            catch { }

            // Seed default DS4Windows configuration for DualSense (portable mode) in the actual exe directory
            try
            {
                var exePath = Directory.GetFiles(targetDir, "DS4Windows.exe", SearchOption.AllDirectories).FirstOrDefault();
                var ds4ExeDir = exePath != null ? Path.GetDirectoryName(exePath)! : targetDir;
                var initializer = new DS4WindowsInitializer(ds4ExeDir);
                initializer.Initialize();
            }
            catch
            {
                // Non-fatal: continue with installation even if seeding fails
            }
            IsDs4Installed = true;
        }

        private void LaunchDs4Windows()
        {
            var baseDir = Path.Combine(AppContext.BaseDirectory, "External Tools", "DS4Windows");
            var exe = Directory.GetFiles(baseDir, "DS4Windows.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (exe == null) throw new FileNotFoundException("DS4Windows.exe not found in External Tools/DS4Windows.");
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
            IsDs4Installed = false;
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
        {
            var baseDir = Path.Combine(AppContext.BaseDirectory, "External Tools", "DS4Windows");
            if (Directory.Exists(baseDir))
            {
                var exe = Directory.GetFiles(baseDir, "DS4Windows.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (exe != null) IsDs4Installed = true;
            }
        }
        private void CheckAutoHotKeyInstallation()
        { if (File.Exists(Path.Combine(AppContext.BaseDirectory, "External Tools", "AutoHotKey", "AutoHotkey64.exe"))) IsAutoHotKeyInstalled = true; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}


