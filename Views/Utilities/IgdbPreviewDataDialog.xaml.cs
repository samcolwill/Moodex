using Moodex.Services.Igdb;
using System.Windows;

namespace Moodex.Views.Utilities
{
    public partial class IgdbPreviewDataDialog : Window
    {
        public IgdbPreviewDataDialog(IgdbGameResult result)
        {
            InitializeComponent();
            DataContext = new IgdbPreviewDataViewModel(result);
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
            => DialogResult = true;
    }

    public class IgdbPreviewDataViewModel
    {
        public string Name { get; }
        public string GenresDisplay { get; }
        public string ReleaseDateDisplay { get; }
        public string Summary { get; }

        public IgdbPreviewDataViewModel(IgdbGameResult result)
        {
            Name = result.Name;
            GenresDisplay = result.Genres.Count > 0
                ? string.Join(", ", result.Genres)
                : "(none)";
            ReleaseDateDisplay = result.ReleaseDate.HasValue
                ? result.ReleaseDate.Value.ToString("MMMM d, yyyy")
                : "(unknown)";
            Summary = !string.IsNullOrWhiteSpace(result.Summary)
                ? result.Summary
                : "(no summary available)";
        }
    }
}
