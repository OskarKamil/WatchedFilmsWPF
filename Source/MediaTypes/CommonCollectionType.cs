using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WatchedFilmsTracker.Source.Models
{
    public class CommonCollectionType
    {
        public List<string> DefaultColumnHeaders { get; set; }
        public string Name { get; set; }

        public Image GetIconImage()
        {
            Image image = new Image
            {
                Source = new BitmapImage(new Uri($"pack://application:,,,/Assets/TabIcons/{Name.ToLower()}.png")),
                Stretch = Stretch.None
            };

            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(image, EdgeMode.Aliased);

            return image;
        }

        public string GetIconImageFilepath()
        {
            return $"pack://application:,,,/Assets/TabIcons/{Name.ToLower()}.png";
        }

        public BitmapImage GetIconImageSource()
        {
            var uri = new Uri(GetIconImageFilepath(), UriKind.Absolute);
            return new BitmapImage(uri);
        }
    }
}