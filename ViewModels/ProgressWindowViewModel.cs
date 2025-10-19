// ProgressWindowViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading;
using System.Threading.Tasks;

namespace SamsGameLauncher.ViewModels
{
    public partial class ProgressWindowViewModel : ObservableObject
    {
        [ObservableProperty] private string title = "Moving Game";
        [ObservableProperty] private double percent;
        [ObservableProperty] private string? currentFile;

        public IAsyncRelayCommand CancelCommand { get; }

        private readonly CancellationTokenSource _cts = new();
        public CancellationToken Token => _cts.Token;

        public ProgressWindowViewModel()
        {
            CancelCommand = new AsyncRelayCommand(CancelAsync);
        }

        private Task CancelAsync()
        {
            _cts.Cancel();          // signal caller
            return Task.CompletedTask;
        }
    }
}