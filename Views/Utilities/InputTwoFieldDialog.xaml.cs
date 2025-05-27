using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SamsGameLauncher.Views.Utilities
{
    public partial class InputTwoFieldDialog : Window, INotifyPropertyChanged
    {
        public string DialogTitle { get; }
        public string Message1 { get; }
        public string Message2 { get; }

        private string _field1Text = string.Empty;
        public string Field1Text
        {
            get => _field1Text;
            set { _field1Text = value; OnPropertyChanged(); }
        }

        private string _field2Text = string.Empty;
        public string Field2Text
        {
            get => _field2Text;
            set { _field2Text = value; OnPropertyChanged(); }
        }

        public InputTwoFieldDialog(
            string title,
            string message1,
            string message2,
            string default1 = "",
            string default2 = "")
        {
            InitializeComponent();
            DialogTitle = title;
            Message1 = message1;
            Message2 = message2;
            Field1Text = default1;
            Field2Text = default2;
            DataContext = this;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}