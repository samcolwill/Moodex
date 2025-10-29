using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Moodex.Views.Utilities
{
    public partial class InputOneFieldDialog : Window, INotifyPropertyChanged
    {
        public string DialogTitle { get; }
        public string Message { get; }

        private string _inputText = "";
        public string InputText
        {
            get => _inputText;
            set { _inputText = value; OnPropertyChanged(); }
        }

        public InputOneFieldDialog(string title, string message, string defaultText = "")
        {
            InitializeComponent();
            DialogTitle = title;
            Message = message;
            InputText = defaultText;
            DataContext = this;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
            => DialogResult = true;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}

