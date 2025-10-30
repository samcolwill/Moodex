// AboutViewModel.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CommunityToolkit.Mvvm.Input;

namespace Moodex.ViewModels.Help
{
    public class AboutViewModel : BaseViewModel
    {
        public string AppName => "Moodex";
        public string Version => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";
        public string UpdatedDate => GetUpdatedDate();

        public string Developer => "Sam Colwill";
        public string WebsiteUrl => "https://samcolwill.com";
        public string GitHubUrl => "https://github.com/samcolwill/Moodex";

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

        private static string GetUpdatedDate()
        {
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    return File.GetLastWriteTime(exePath).ToString("yyyy-MM-dd");
                }
            }
            catch
            {
                // fall through to now
            }
            return DateTime.Now.ToString("yyyy-MM-dd");
        }
    }
}

