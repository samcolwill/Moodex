using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Moodex.ViewModels.Help
{
    public class GettingStartedWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<GettingStartedSection> Sections { get; }

        private GettingStartedSection _selectedSection;
        public GettingStartedSection SelectedSection
        {
            get => _selectedSection;
            set
            {
                if (_selectedSection != value)
                {
                    _selectedSection = value;
                    OnPropertyChanged(nameof(SelectedSection));
                }
            }
        }

        public GettingStartedWindowViewModel()
        {
            Sections = new ObservableCollection<GettingStartedSection>
            {
                new GettingStartedSection("1. Welcome", new GettingStarted.Step1ViewModel()),
                new GettingStartedSection("2. Adding an emulator", new GettingStarted.Step2ViewModel()),
                new GettingStartedSection("3. Adding games", new GettingStarted.Step3ViewModel()),
                new GettingStartedSection("4. Right-click menu", new GettingStarted.Step4ViewModel()),
                new GettingStartedSection("5. Get playing", new GettingStarted.Step5ViewModel())
            };
            _selectedSection = Sections[0];
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class GettingStartedSection
    {
        public string Name { get; }
        public object ViewModel { get; }

        public GettingStartedSection(string name, object viewModel)
        {
            Name = name;
            ViewModel = viewModel;
        }
    }
}


