using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Moodex.Views.Utilities
{
    public partial class IgdbPreviewCoverDialog : Window
    {
        public IgdbPreviewCoverDialog(byte[] imageBytes)
        {
            InitializeComponent();

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(imageBytes);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            CoverImage.Source = bitmap;
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
            => DialogResult = true;
    }
}
