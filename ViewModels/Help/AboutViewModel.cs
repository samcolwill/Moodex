// AboutViewModel.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CommunityToolkit.Mvvm.Input;

namespace SamsGameLauncher.ViewModels.Help
{
    public class AboutViewModel : BaseViewModel
    {
        public string AppName => "Sam’s Game Launcher";
        public string Version => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";
        public string UpdatedDate
            => File.Exists(Assembly.GetEntryAssembly().Location)
               ? File.GetLastWriteTime(Assembly.GetEntryAssembly().Location)
                     .ToString("yyyy-MM-dd")
               : DateTime.Now.ToString("yyyy-MM-dd");

        public string Developer => "Sam Colwill";
        public string WebsiteUrl => "https://samcolwill.com";
        public string GitHubUrl => "https://github.com/samcolwill/SamsGameLauncher";

        public IRelayCommand OpenWebsiteCommand { get; }
        public IRelayCommand OpenGitHubCommand { get; }

        public AboutViewModel()
        {
            OpenWebsiteCommand = new RelayCommand(() => OpenLink(WebsiteUrl));
            OpenGitHubCommand = new RelayCommand(() => OpenLink(GitHubUrl));
        }

        private void OpenLink(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
