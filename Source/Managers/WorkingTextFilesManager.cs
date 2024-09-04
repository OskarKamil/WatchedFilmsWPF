using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WatchedFilmsTracker.Source.ManagingFilmsFile;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Managers
{
    internal class WorkingTextFilesManager
    {
        public static ObservableCollection<TabItem> TabItemsWorkingFiles { get; set; } = new ObservableCollection<TabItem>();
        public static ObservableCollection<WorkingTextFile> WorkingTextFiles { get; set; } = new ObservableCollection<WorkingTextFile>();


        public static void CreateNewWorkingTextFile(CommonCollection commonCollection)
        {
            WorkingTextFile workingTextFile = new WorkingTextFile(commonCollection);
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

            Grid grid = new Grid
            {
                Name = "grid"
            };

            DataGrid dataGridMainWindow = new DataGrid
            {
                Name = "dataGridMainWindow",
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                ColumnWidth = DataGridLength.Auto,
                CanUserDeleteRows = false,
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(0xD6, 0xE2, 0xFF)),
                AlternationCount = 4,
                CanUserResizeRows = false,
                SelectionUnit = DataGridSelectionUnit.CellOrRowHeader,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Visible
            };

            // Set ScrollViewer properties on the DataGrid
            ScrollViewer.SetCanContentScroll(dataGridMainWindow, true);
            ScrollViewer.SetVerticalScrollBarVisibility(dataGridMainWindow, ScrollBarVisibility.Auto);
            ScrollViewer.SetHorizontalScrollBarVisibility(dataGridMainWindow, ScrollBarVisibility.Auto);

            // Add the DataGrid to the Grid
            grid.Children.Add(dataGridMainWindow);

            tabItem.Content = grid;
            tabItem.IsSelected = true;

            TabItemsWorkingFiles.Add(tabItem);
            Debug.WriteLine("new tab should be added");
            WorkingTextFiles.Add(workingTextFile);
        }
    }
}