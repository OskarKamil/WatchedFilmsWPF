using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WatchedFilmsTracker.Source.Models
{
    public static class CommonCollections
    {
        public static Dictionary<string, CommonCollection> CommonCollectionsSet { get; } = new Dictionary<string, CommonCollection>();
        private static List<CommonCollection> _sortedCollections;

        static CommonCollections()
        {
            foreach (CollectionType collectionType in Enum.GetValues(typeof(CollectionType)))
            {
                Image image = new Image
                {
                    Source = new BitmapImage(new Uri($"pack://application:,,,/Assets/TabIcons/{collectionType.ToString().ToLower()}.png")),
                    Stretch = Stretch.None
                };

                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
                RenderOptions.SetEdgeMode(image, EdgeMode.Aliased);

                string collectionTypeName = collectionType.ToString();

                CommonCollection collection = new CommonCollection()
                {
                    Name = collectionTypeName,
                    IconImage = image
                };

                CommonCollectionsSet.Add(collectionTypeName, collection);
            }
        }

        public enum CollectionType
        {
            Books,
            Comics,
            Films,
            Games,
            Other,
            Series,
        }

        public static IEnumerable<CommonCollection> GetAllCollectionsAlphabetically()
        {
            if (_sortedCollections == null)
            {
                _sortedCollections = CommonCollectionsSet
               .OrderBy(kv => kv.Key)
               .Select(kv => kv.Value)
               .ToList();
            }
            return _sortedCollections;
        }
    }
}