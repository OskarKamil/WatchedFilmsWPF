using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WatchedFilmsTracker.Source.Managers
{
    internal class TabRepresentationFile
    {
        private CollectionType collectionType { get; set; }
        private Image tabImage { get; set; }

        public TabRepresentationFile(CollectionType collectionType)
        {
            this.collectionType = collectionType;
            CreateTab();
        }

        public enum CollectionType
        {
            Films,
            Games,
            Series,
            Books
        }

        public TabItem CreateTab()
        {
            TabItem item = new TabItem();

            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
            };

            Image tabImage = new Image
            {
                Source = new BitmapImage(new Uri($"/Assets/TabIcons/{collectionType.ToString().ToLower()}.png", UriKind.Relative)),
                Stretch = Stretch.None
            };
            RenderOptions.SetBitmapScalingMode(tabImage, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(tabImage, EdgeMode.Aliased);

            TextBlock textBlock = new TextBlock
            {
                Text = collectionType.ToString()
            };

            stackPanel.Children.Add(tabImage);
            stackPanel.Children.Add(textBlock);

            item.Header = stackPanel;

            return item;
        }
    }
}