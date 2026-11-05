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

        public static string NormalizePath(string filePath) => Path.GetFullPath(filePath);

        /// <summary>Finds a saved-path collection without treating separate untitled tabs as duplicates.</summary>
        public static CollectionFile? FindOpenFile(string? filePath, CollectionFile? excluding = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            string fullPath = NormalizePath(filePath);
            return OpenedFiles.FirstOrDefault(file => !ReferenceEquals(file, excluding) &&
                !string.IsNullOrWhiteSpace(file.Filepath) &&
                string.Equals(NormalizePath(file.Filepath), fullPath, StringComparison.OrdinalIgnoreCase));
        }

        public static void SelectFile(CollectionFile file)
        {
            int index = OpenedFiles.IndexOf(file);
            if (index < 0 || index >= TabsOfOpenedFiles.Count)
                return;

            TabItem tab = TabsOfOpenedFiles[index];
            tab.IsSelected = true;
            if (TabControl != null)
                TabControl.SelectedItem = tab;
        }

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
            AddWorkingFile(new CollectionFile(commonCollection));
        }

        public static void CreateNewWorkingFile(string filePath)
        {
            string fullPath = NormalizePath(filePath);
            if (FindOpenFile(fullPath) is CollectionFile existing)
            {
                SelectFile(existing);
                return;
            }

            AddWorkingFile(new CollectionFile(fullPath));
        }

        private static void AddWorkingFile(CollectionFile workingTextFile)
        {
            OpenedFiles.Add(workingTextFile);

            var newTab = CreateNewTab(workingTextFile);

            TabsOfOpenedFiles.Add(newTab);

            newTab.Content = workingTextFile.Grid;
            SelectFile(workingTextFile);

            NewFileLoaded?.Invoke(null, new NewFileLoadedEventArgs(workingTextFile));
            workingTextFile.CommonCollectionTypeChanged += UpdateTabIconAndText;
            workingTextFile.UnsavedChangesChanged += UpdateTabText;
            workingTextFile.FileSaved += UpdateTabText;

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
            if (indexOfTab < 0 || indexOfTab >= OpenedFiles.Count)
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
            tabItem.ToolTip = string.IsNullOrWhiteSpace(workingTextFile.Filepath)
                ? "New collection (not saved)"
                : workingTextFile.Filepath;

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
            if (sender is not CollectionFile workingTextFile)
                return;
            int tabIndex = OpenedFiles.IndexOf(workingTextFile);
            if (tabIndex >= 0 && tabIndex < TabsOfOpenedFiles.Count)
            {
                TabItem tabItem = TabsOfOpenedFiles[tabIndex];
                TextBlock textBlock = GetTextBlockFromTab(tabItem);

                if (textBlock != null)
                {
                    string name = string.IsNullOrWhiteSpace(workingTextFile.Filepath)
                        ? "new collection"
                        : Path.GetFileName(workingTextFile.Filepath);
                    textBlock.Text = (workingTextFile.HasUnsavedChanges ? "* " : string.Empty) + name;
                }
                tabItem.ToolTip = string.IsNullOrWhiteSpace(workingTextFile.Filepath)
                    ? "New collection (not saved)"
                    : workingTextFile.Filepath;
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
