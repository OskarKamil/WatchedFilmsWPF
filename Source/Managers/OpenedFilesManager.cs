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
    internal class OpenedFilesManager
    {
        public static event EventHandler<NewFileLoadedEventArgs>? NewFileLoaded;

        public static ObservableCollection<CollectionFile> OpenedFiles { get; set; } = new ObservableCollection<CollectionFile>();
        public static TabControl? TabControl { get; set; }

        public static ObservableCollection<TabItem> TabsOfOpenedFiles { get; set; } = new ObservableCollection<TabItem>();

        public static void CloseTab(CollectionFile workingTextFile)
        {
            int index = OpenedFiles.IndexOf(workingTextFile);
            if (index >= 0)
            {
                if (TabsOfOpenedFiles.Count == 1)
                {
                    CreateEmptyWorkingFile(CommonCollections.GetCommonCollectionByName(CommonCollections.CollectionType.Films));
                    TabsOfOpenedFiles[1].IsSelected = true;
                }
                else if (TabsOfOpenedFiles.Count - 1 > index)
                {
                    TabsOfOpenedFiles[index + 1].IsSelected = true;
                }
                else if (TabsOfOpenedFiles.Count - 1 == index)
                {
                    TabsOfOpenedFiles[index - 1].IsSelected = true;
                }
                OpenedFiles.RemoveAt(index);
                TabsOfOpenedFiles.RemoveAt(index);
            }
        }

        public static void CreateEmptyWorkingFile(CommonCollectionType commonCollection)
        {
            CollectionFile workingTextFile = new CollectionFile(commonCollection);
            OpenedFiles.Add(workingTextFile);

            var newTab = CreateNewTab(workingTextFile);

            TabsOfOpenedFiles.Add(newTab);

            newTab.Content = workingTextFile.Grid;
            newTab.IsSelected = true;

            NewFileLoaded?.Invoke(null, new NewFileLoadedEventArgs(workingTextFile));
            workingTextFile.CommonCollectionTypeChanged += UpdateTabIconAndText;
            workingTextFile.UnsavedChangesChanged += UpdateTabText;

            workingTextFile.FileClosing += (sender, e) =>
            {
                CloseTab((CollectionFile)sender);
            };
        }

        public static void CreateNewWorkingFile(string filePath)
        {
            CollectionFile workingTextFile = new CollectionFile(filePath);
            OpenedFiles.Add(workingTextFile);

            var newTab = CreateNewTab(workingTextFile);

            TabsOfOpenedFiles.Add(newTab);

            newTab.Content = workingTextFile.Grid;
            newTab.IsSelected = true;

            NewFileLoaded?.Invoke(null, new NewFileLoadedEventArgs(workingTextFile));
            workingTextFile.CommonCollectionTypeChanged += UpdateTabIconAndText;
            workingTextFile.UnsavedChangesChanged += UpdateTabText;

            workingTextFile.FileClosing += (sender, e) =>
            {
                CloseTab((CollectionFile)sender);
            };
        }

        public static TabItem CurrentlyOpenedTabItem()
        {
            return (TabItem)TabControl.SelectedItem;
        }

        public static CollectionFile CurrentlyOpenedWorkingFile()
        {
            if (TabControl?.SelectedItem is not TabItem selectedTab)
                return null;
            int indexOfTab = TabsOfOpenedFiles.IndexOf(selectedTab);
            if (indexOfTab == -1)
                return null;
            // Debug.WriteLine($"index of tab is {indexOfTab}, and number of tabs is: {TabsOfOpenedFiles.Count}, or {TabControl.Items.Count} or {OpenedFiles.Count}");
            return OpenedFiles[indexOfTab];
        }

        private static TabItem CreateNewTab(CollectionFile workingTextFile)
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

        private static void UpdateTabIconAndText(object? sender, CollectionFile.CommonCollectionTypeChangedEventArgs e)
        {
            if (sender is not CollectionFile file)
                return;
            int index = OpenedFiles.IndexOf(file);
            if (index >= 0 && GetImageFromTab(TabsOfOpenedFiles[index]) is Image image)
                image.Source = e.NewCommonCollectionType.GetIconImageSource();
        }

        private static void UpdateTabText(object? sender, EventArgs e)
        {
            CollectionFile workingTextFile = (CollectionFile)sender;
            int tabIndex = OpenedFiles.IndexOf(workingTextFile);
            if (tabIndex != -1)
            {
                TabItem tabItem = TabsOfOpenedFiles[tabIndex];
                TextBlock textBlock = GetTextBlockFromTab(tabItem);

                if (textBlock != null)
                {
                    string currentTabText = textBlock.Text;

                    if (workingTextFile.HasUnsavedChanges)
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
            public NewFileLoadedEventArgs(CollectionFile workingTextFile)
            {
                NewWorkingTextFile = workingTextFile;
            }

            public CollectionFile NewWorkingTextFile { get; }
        }
    }
}
