using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.ManagingFilmsFile;
using WatchedFilmsTracker.Source.Models;
using static WatchedFilmsTracker.Source.Models.CommonCollections;

namespace WatchedFilmsTracker.Source.Managers
{
    internal class WorkingTextFilesManager
    {
        public static MainWindow MainWindow { get; set; }
        public static TabControl TabControl { get; set; }
        public static ObservableCollection<TabItem> TabItemsWorkingFiles { get; set; } = new ObservableCollection<TabItem>();
        public static ObservableCollection<WorkingTextFile> WorkingTextFiles { get; set; } = new ObservableCollection<WorkingTextFile>();

        public static void CreateEmptyWorkingFile(CommonCollectionType commonCollection)
        {
            WorkingTextFile workingTextFile = new WorkingTextFile(commonCollection);
            WorkingTextFiles.Add(workingTextFile);

            var newTab = CreateNewTab(commonCollection);

            newTab.Content = workingTextFile.Grid;
            newTab.IsSelected = true;

            TabItemsWorkingFiles.Add(newTab);
        }

        public static void CreateNewWorkingFile(string filePath)
        {
            WorkingTextFile workingTextFile = new WorkingTextFile(filePath);
            WorkingTextFiles.Add(workingTextFile);

            var newTab = CreateNewTab(GetCommonCollectionByName(CollectionType.Other));

            TabItemsWorkingFiles.Add(newTab);

            newTab.Content = workingTextFile.Grid;
            newTab.IsSelected = true;
        }

        public static WorkingTextFile CurrentlyOpenedWorkingFile()
        {
            var selectedTab = (TabItem)TabControl.SelectedItem;
            int indexOfTab = TabItemsWorkingFiles.IndexOf(selectedTab);
            Debug.WriteLine($"index of tab is {indexOfTab}, and number of tabs is: {TabItemsWorkingFiles.Count}, or {TabControl.Items.Count} or {WorkingTextFiles.Count}");
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