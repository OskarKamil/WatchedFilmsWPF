using System.Collections.ObjectModel;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.ManagingFilmsFile;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Managers
{
    internal class WorkingTextFilesManager
    {
        public static MainWindow MainWindow { get;  set; }

        public static TabControl TabControl { get; set; }
        public static ObservableCollection<TabItem> TabItemsWorkingFiles { get; set; } = new ObservableCollection<TabItem>();
        public static ObservableCollection<WorkingTextFile> WorkingTextFiles { get; set; } = new ObservableCollection<WorkingTextFile>();

        public static void CreateEmptyWorkingFile(CommonCollectionType commonCollection)
        {
            WorkingTextFile workingTextFile = new WorkingTextFile(commonCollection);
            var newTab = CreateNewTab(commonCollection);

            newTab.Content = workingTextFile.Grid;
            newTab.IsSelected = true;

            TabItemsWorkingFiles.Add(newTab);
            WorkingTextFiles.Add(workingTextFile);
        }

        public static WorkingTextFile CurrentlyOpenedWorkingFile()
        {
            var selectedTab = (TabItem)TabControl.SelectedItem;
            int indexOfTab = TabItemsWorkingFiles.IndexOf(selectedTab);
            return WorkingTextFiles[indexOfTab];
        }

        private static TabItem CreateNewTab(CommonCollectionType commonCollection)
        {
            TabItem tabItem = new TabItem();
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            Image headerImage = commonCollection.GetIconImage();
            TextBlock textBlock = new TextBlock
            {
                Text = commonCollection.Name
            };

            stackPanel.Children.Add(headerImage);
            stackPanel.Children.Add(textBlock);

            tabItem.Header = stackPanel;

            return tabItem;
        }
    }
}