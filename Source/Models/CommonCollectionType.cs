using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WatchedFilmsTracker.Source.Models
{
    public sealed class CommonCollectionType
    {
        public List<string> DefaultColumnHeaders { get; set; } = new();
        public string Name { get; set; } = "Other";

        public Image GetIconImage()
        {
            Image image = new Image
            {
                Source = GetIconImageSource(),
                Stretch = Stretch.None
            };

            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(image, EdgeMode.Aliased);

            return image;
        }

        public string GetIconImageFilepath()
        {
            string iconName = CommonCollections.CommonCollectionsSet.TryGetValue(Name, out CommonCollectionType? known)
                ? known.Name.ToLowerInvariant() : "other";
            return $"pack://application:,,,/Assets/TabIcons/{iconName}.png";
        }

        public BitmapImage GetIconImageSource()
        {
            var uri = new Uri(GetIconImageFilepath(), UriKind.Absolute);
            return new BitmapImage(uri);
        }
    }
}
