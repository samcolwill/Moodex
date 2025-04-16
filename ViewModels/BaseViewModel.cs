using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SamsGameLauncher.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        // Fired when a property's value changes
        public event PropertyChangedEventHandler? PropertyChanged;

        // Invoke this in property setters to update the UI
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}