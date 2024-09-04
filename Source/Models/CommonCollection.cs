using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WatchedFilmsTracker.Source.Models
{
    public class CommonCollection
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
    }
}