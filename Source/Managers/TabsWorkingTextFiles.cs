using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using WatchedFilmsTracker.Source.Helpers;
using WatchedFilmsTracker.Source.ManagingFilmsFile;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Managers
{
    internal class TabsWorkingTextFiles
    {
        public static MainWindow MainWindow { get; set; }

        public static TabControl TabControl { get; set; }

        public static ObservableCollection<TabItem> TabItemsWorkingFiles { get; set; } = new ObservableCollection<TabItem>();

        public static ObservableCollection<WorkingTextFile> WorkingTextFiles { get; set; } = new ObservableCollection<WorkingTextFile>();

        public static event EventHandler<NewFileLoadedEventArgs> NewFileLoaded;

        public static void CloseTab(WorkingTextFile workingTextFile)
        {
            int index = WorkingTextFiles.IndexOf(workingTextFile);
            if (index >= 0)
            {
                if (TabItemsWorkingFiles.Count == 1)
                {
                    CreateEmptyWorkingFile(CommonCollections.GetCommonCollectionByName(CommonCollections.CollectionType.Films));
                    TabItemsWorkingFiles[1].IsSelected = true;
                }
                else if (TabItemsWorkingFiles.Count - 1 > index)
                {
                    TabItemsWorkingFiles[index + 1].IsSelected = true;
                }
                else if (TabItemsWorkingFiles.Count - 1 == index)
                {
                    TabItemsWorkingFiles[index - 1].IsSelected = true;
                }
                WorkingTextFiles.RemoveAt(index);
                TabItemsWorkingFiles.RemoveAt(index);
            }
        }

        public static void CreateEmptyWorkingFile(CommonCollectionType commonCollection)
        {
            WorkingTextFile workingTextFile = new WorkingTextFile(commonCollection);
            WorkingTextFiles.Add(workingTextFile);

            var newTab = CreateNewTab(workingTextFile);

            TabItemsWorkingFiles.Add(newTab);

            newTab.Content = workingTextFile.Grid;
            newTab.IsSelected = true;

            NewFileLoaded?.Invoke(null, new NewFileLoadedEventArgs(workingTextFile));
            workingTextFile.CommonCollectionTypeChanged += UpdateTabIconAndText;
            workingTextFile.CollectionHasChanged += UpdateTabText;
            workingTextFile.SavedComplete += UpdateTabText;

            workingTextFile.FileClosing += (sender, e) =>
            {
                CloseTab((WorkingTextFile)sender);
            };
        }

        public static void CreateNewWorkingFile(string filePath)
        {
            WorkingTextFile workingTextFile = new WorkingTextFile(filePath);
            WorkingTextFiles.Add(workingTextFile);

            var newTab = CreateNewTab(workingTextFile);

            TabItemsWorkingFiles.Add(newTab);

            newTab.Content = workingTextFile.Grid;
            newTab.IsSelected = true;

            NewFileLoaded?.Invoke(null, new NewFileLoadedEventArgs(workingTextFile));
            workingTextFile.CommonCollectionTypeChanged += UpdateTabIconAndText;
            workingTextFile.CollectionHasChanged += UpdateTabText;
            workingTextFile.SavedComplete += UpdateTabText;

            workingTextFile.FileClosing += (sender, e) =>
            {
                CloseTab((WorkingTextFile)sender);
            };
        }

        public static TabItem CurrentlyOpenedTabItem()
        {
            return (TabItem)TabControl.SelectedItem;
        }

        public static WorkingTextFile CurrentlyOpenedWorkingFile()
        {
            var selectedTab = (TabItem)TabControl.SelectedItem;
            int indexOfTab = TabItemsWorkingFiles.IndexOf(selectedTab);
            if (indexOfTab == -1)
                return null;
            // Debug.WriteLine($"index of tab is {indexOfTab}, and number of tabs is: {TabItemsWorkingFiles.Count}, or {TabControl.Items.Count} or {WorkingTextFiles.Count}");
            return WorkingTextFiles[indexOfTab];
        }

        private static TabItem CreateNewTab(WorkingTextFile workingTextFile)
        {
            TabItem tabItem = new TabItem();
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            Image headerImage = workingTextFile.CommonCollectionType.GetIconImage();

            string tabText;
            if (string.IsNullOrEmpty(workingTextFile.Filepath))
                tabText = "new collection";
            else
                tabText = Path.GetFileName(workingTextFile.Filepath);

            TextBlock textBlock = new TextBlock
            {
                Text = tabText,
                Margin = new System.Windows.Thickness(5, 0, 5, 0)
            };

            Image closeTabButtonImage = ImageHelper.GetPixelImageFromAssets("TabIcons/closeX.png");
            Button closeTabButton = new Button();

            closeTabButton.Click += (sender, e) =>
            {
                Debug.WriteLine("button close tab clicke");
                workingTextFile.TryToCloseFile();
            };
            tabItem.MouseDown += (sender, e) =>
            {
                if (e.MiddleButton == MouseButtonState.Pressed)
                    workingTextFile.TryToCloseFile();
            };

            closeTabButton.Content = closeTabButtonImage;

            stackPanel.Children.Add(headerImage);
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(closeTabButton);

            tabItem.Header = stackPanel;

            return tabItem;
        }

        private static Image GetImageFromTab(TabItem tabItem)
        {
            StackPanel headerPanel = tabItem.Header as StackPanel;

            if (headerPanel != null)
            {
                Image headerImage = headerPanel.Children[0] as Image;

                if (headerImage != null)
                {
                    return headerImage;
                }
            }

            return null;
        }

        private static TextBlock GetTextBlockFromTab(TabItem tabItem)
        {
            StackPanel headerPanel = tabItem.Header as StackPanel;

            if (headerPanel != null)
            {
                TextBlock headerText = headerPanel.Children[1] as TextBlock;

                if (headerText != null)
                {
                    return headerText;
                }
            }

            return null;
        }

        private static void UpdateTabIconAndText(object? sender, WorkingTextFile.CommonCollectionTypeChangedEventArgs e)
        {
            TabItem tabItem = CurrentlyOpenedTabItem();
            GetImageFromTab(tabItem).Source = e.NewCommonCollectionType.GetIconImageSource();
        }

        private static void UpdateTabText(object? sender, EventArgs e)
        {
            WorkingTextFile workingTextFile = (WorkingTextFile)sender;
            int tabIndex = WorkingTextFiles.IndexOf(workingTextFile);
            if (tabIndex != -1)
            {
                TabItem tabItem = TabItemsWorkingFiles[tabIndex];
                TextBlock textBlock = GetTextBlockFromTab(tabItem);

                if (textBlock != null)
                {
                    string currentTabText = textBlock.Text;

                    if (workingTextFile.UnsavedChanges)
                    {
                        if (!currentTabText.StartsWith("* "))
                        {
                            textBlock.Text = "* " + currentTabText;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("unsaved changes no");

                        if (currentTabText.StartsWith("* "))
                        {
                            textBlock.Text = currentTabText.Substring(2);
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("textlbock IS null");
                }
            }
        }

        public class NewFileLoadedEventArgs : EventArgs
        {
            public WorkingTextFile NewWorkingTextFile { get; }

            public NewFileLoadedEventArgs(WorkingTextFile workingTextFile)
            {
                NewWorkingTextFile = workingTextFile;
            }
        }
    }
}