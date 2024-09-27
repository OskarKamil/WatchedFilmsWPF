using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WatchedFilmsTracker.Source.Helpers
{
    internal class ImageHelper
    {
        private const string AssetsPath = "pack://application:,,,/Assets/";

        public static Image GetPixelImageFromAssets(string imageFilepathFromAssets)
        {
            Image image = new Image
            {
                Source = new BitmapImage(new Uri($"{AssetsPath}{imageFilepathFromAssets}")),
                Stretch = Stretch.None
            };

            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(image, EdgeMode.Aliased);

            return image;
        }
    }
}