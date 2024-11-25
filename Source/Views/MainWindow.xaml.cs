using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WatchedFilmsTracker.Source;
using WatchedFilmsTracker.Source.BackgroundServices;
using WatchedFilmsTracker.Source.Buttons;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.ManagingFilmsFile;
using WatchedFilmsTracker.Source.Models;
using WatchedFilmsTracker.Source.Statistics;
using static WatchedFilmsTracker.Source.BackgroundServices.CheckForUpdateService;

namespace WatchedFilmsTracker
{
    public partial class MainWindow : Window
    {
        private const string SearchBoxDefaultText = "Filter results";
        private CancellationTokenSource cancellationTokenSourceForDecadalStatistics;
        private CancellationTokenSource cancellationTokenSourceForYearlyStatistics;
        private DecadalStatisticsTableManager decadalStatisticsTableManager;
        private DataGridManager filmsColumnsManager;
        private LocalFilmsFilesService localFilmsFilesService;
        private SearchManager searchManager;
        private MainWindowViewModel viewModel;

        private YearlyStatisticsTableManager yearlyStatisticsTableManager;

        public MainWindow()
        {
            //General GUI adjustments
            InitializeComponent(); // must be first line always
            viewModel = new MainWindowViewModel();
            this.DataContext = viewModel;

            searchTextBox.Text = SearchBoxDefaultText;

            Closing += MainWindow_Closing; // override closing window

            //BUTTON MANAGER
            ButtonManager.AlwaysActiveButtons.Add(buttonOpenFile);
            ButtonManager.AlwaysActiveButtons.Add(buttonAbout);
            ButtonManager.AlwaysActiveButtons.Add(buttonSaveAs);
            ButtonManager.AlwaysActiveButtons.Add(ButtonNewFile);
            ButtonManager.AlwaysActiveButtons.Add(buttonOpenLocally);

            ButtonManager.UnsavedChangeButtons.Add(buttonSave);

            ButtonManager.OpenedFileButtons.Add(buttonAddRecord);
            ButtonManager.OpenedFileButtons.Add(buttonClearAll);
            ButtonManager.OpenedFileButtons.Add(ButtonCurrentFileCollectionType);

            ButtonManager.AtLeastOneRecordButtons.Add(buttonSelectLast);

            ButtonManager.SelectedCellsButtons.Add(buttonDeleteFilmRecord);
            ButtonManager.SelectedCellsButtons.Add(buttoRemoveColumn);
            ButtonManager.SelectedCellsButtons.Add(buttoRenameColumn);

            ButtonManager.AnyChangeButtons.Add(buttonRevertChanges);

            ButtonManager.FileExistsOnDiskButtons.Add(buttonOpenContainingFolder);

            ButtonManager.FileIsNotInLocalMyDataDirectoryButtons.Add(buttonSaveLocally);

            NewFileActionPrepareContextMenu();
            CurrentlyOpenedFilePrepareContextMenu();

            //SETTINGS
            SettingsManager.LoadDefaultSettings();
            SettingsManager.LoadSettingsFromConfigFile();
            ApplyUserSettingsToTheProgram(); // window size, position, last path, other settings

            //CHECK UPDATE ON STARTUP
            if (SettingsManager.CheckUpdateOnStartup)
                ManualCheckForUpdate(CheckUpdatesButton, null);

            //FILEMANAGER
            //   workingTextFile.DeleteRecordAction = DeleteFilmRecord_ButtonClick;
            OpenLastOpenedFiles(SettingsManager.LastPath);

            //LOCAL FILES SERVICE
            // localFilmsFilesService = new LocalFilmsFilesService(workingTextFile);
            // LocalFilmsFilesService.CreateMyDataFolderIfNotExist();

            //STATISTICS DISPLAY COLUMNS
            decadalStatisticsTableManager = new DecadalStatisticsTableManager(decadalGrid);
            yearlyStatisticsTableManager = new YearlyStatisticsTableManager(yearlyGrid);

            //TABCONTROL
            TabsWorkingTextFiles.TabControl = TabControlMainWindow;
            TabsWorkingTextFiles.MainWindow = this;
            TabControlMainWindow.ItemsSource = TabsWorkingTextFiles.TabItemsWorkingFiles;

            TabControlMainWindow.SelectionChanged += (s, e) =>
            {
                UpdateButtons();
                UpdateStageTitle();
                UpdateFileInformation();
                UpdateCommonCollectionElements(null, null);
            };

            TabsWorkingTextFiles.NewFileLoaded += (sender, e) =>
            {
                e.NewWorkingTextFile.CollectionHasChanged += UpdateStageTitle;
                e.NewWorkingTextFile.CommonCollectionTypeChanged += UpdateCommonCollectionElements;
                e.NewWorkingTextFile.SavedComplete += UpdateStageTitle;
            };

            //SNAPSHOT SERVICE
            // FileChangesSnapshotService.CreateSnapshotFolderIfNotExist();
            // FileChangesSnapshotService.FileManager = workingTextFile;
            //  FileChangesSnapshotService.SubscribeToSaveCompletedEvent(this);

            //SEARCH MANAGER
            //    searchManager = new SearchManager(workingTextFile, searchTextBox, dataGridMainWindow);
        }

        public event EventHandler FileOpened;

        public void ButtonDeleteFilmRecord_Click(object sender, RoutedEventArgs e) // RemoveRecord, DeleteRecord
        {
            if (GetCurrentlyOpenedTabWorkingTextFile().HasSelectedCells())
            {
                RecordModel selected = GetCurrentlyOpenedTabWorkingTextFile().DataGrid.SelectedCells[0].Item as RecordModel;
                GetCurrentlyOpenedTabWorkingTextFile().CollectionOfRecords.DeleteRecordFromList(selected);
            }
        }

        public WorkingTextFile CurrentWorkingFile()
        {
            return TabsWorkingTextFiles.CurrentlyOpenedWorkingFile();
        }

        public WorkingTextFile GetCurrentlyOpenedTabWorkingTextFile()
        {
            WorkingTextFile currentlyOpened = TabsWorkingTextFiles.CurrentlyOpenedWorkingFile();

            TextBoxCommentsBefore.DataContext = currentlyOpened.Metadata;
            TextBoxProgramComments.DataContext = currentlyOpened.Metadata;
            TextBoxCommentsAfter.DataContext = currentlyOpened.Metadata;
            TextBoxFilePath.DataContext = currentlyOpened;
            LabelAverageRatingRecord.DataContext = currentlyOpened.CollectionStatistics;
            LabelTotalRecordsNumber.DataContext = currentlyOpened.CollectionStatistics;
            ItemsColumnsDataTypes.DataContext = currentlyOpened.CollectionStatistics.DataGridInfo;

            return currentlyOpened;
        }

        public void UpdateStageTitle()
        {
            string stageTitle = "";

            if (GetCurrentlyOpenedTabWorkingTextFile().UnsavedChanges)
            {
                stageTitle = "*";
            }

            if (GetCurrentlyOpenedTabWorkingTextFile().CollectionOfRecords is null || string.IsNullOrEmpty(GetCurrentlyOpenedTabWorkingTextFile().Filepath))
            {
                stageTitle += "New File" + " - " + ProgramInformation.PROGRAM_NAME;
            }
            else
            {
                stageTitle += GetCurrentlyOpenedTabWorkingTextFile().Filepath + " - " + ProgramInformation.PROGRAM_NAME;
            }

            this.Title = stageTitle;
        }

        public async Task UpdateStatistics()
        {
            //   UpdateNumberOfFilms();
            //   UpdateAverageFilmRating();

            // Heavy tasks
            //  var decadalTask = Task.Run(() => UpdateReportDecadalStatistics());
            //   var yearlyTask = Task.Run(() => UpdateReportYearlyStatistics());

            try
            {
                // Await both tasks to complete
                //  await Task.WhenAll(decadalTask, yearlyTask);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while updating statistics: {ex}");
            }
        }

        private void AboutButton(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("About dialog opened");
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this; // Set the owner window to enable modal behavior
            aboutWindow.ShowDialog();
        }

        private void ButtonAddColumn_Click(object sender, RoutedEventArgs e)
        {
            TabsWorkingTextFiles.CurrentlyOpenedWorkingFile().CollectionOfRecords.CreateNewColumn("Column");
        }

        private void ApplyUserSettingsToTheProgram()
        {
            autosaveBox.IsChecked = SettingsManager.AutoSave;
            defaultDateBox.IsChecked = SettingsManager.DefaultDateIsToday;
            checkboxCheckForUpdatesStartup.IsChecked = SettingsManager.CheckUpdateOnStartup;
            checkboxScrollLastPosition.IsChecked = SettingsManager.ScrollLastPosition;
            this.Left = SettingsManager.WindowLeft;
            this.Top = SettingsManager.WindowTop;
            this.Width = SettingsManager.WindowWidth;
            this.Height = SettingsManager.WindowHeight;
        }

        private void ButtonAddRecord_Action(object sender, RoutedEventArgs e) // AddRecord, NewRecord
        {
            TabsWorkingTextFiles.CurrentlyOpenedWorkingFile().CollectionOfRecords.AddEmptyRecordToList();
        }

        private void ButtonCurrentFileCollectionType_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            button.ContextMenu.IsOpen = true;
        }

        private void ButtonNewFile_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            button.ContextMenu.IsOpen = true;
        }

        private void CheckBoxAutoSave(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            SettingsManager.AutoSave = (bool)checkBox.IsChecked;
            checkBox.IsChecked = SettingsManager.AutoSave;
        }

        // not in use for now
        //private void BuildDynamicStatistics()
        //{
        //    UpdateReportDecadalStatistics();
        //    UpdateReportYearlyStatistics();
        //}
        private void CheckBoxDefaultDate(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            SettingsManager.DefaultDateIsToday = (bool)checkBox.IsChecked;
            checkBox.IsChecked = SettingsManager.DefaultDateIsToday;
        }

        private void CheckBoxLastFilmPosition(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            SettingsManager.ScrollLastPosition = (bool)checkBox.IsChecked;
            checkBox.IsChecked = SettingsManager.ScrollLastPosition;
        }

        private void CheckBoxUpdateStartup(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)(sender);
            SettingsManager.CheckUpdateOnStartup = (bool)checkBox.IsChecked;
        }

        private void ClearAll(object sender, RoutedEventArgs e)
        {
            GetCurrentlyOpenedTabWorkingTextFile().CollectionOfRecords.DeleteAllRecords();
        }

        private void CurrentlyOpenedFilePrepareContextMenu()
        {
            ContextMenu contextMenu = new ContextMenu();

            foreach (CommonCollectionType commonCollection in CommonCollections.GetAllCollectionsAlphabetically())
            {
                MenuItem menuItem = new MenuItem
                {
                    Header = commonCollection.Name,
                    Icon = commonCollection.GetIconImage(),
                };

                menuItem.Click += (sender, e) =>
                {
                    GetCurrentlyOpenedTabWorkingTextFile().CommonCollectionType = commonCollection;
                };
                contextMenu.Items.Add(menuItem);
            }

            ButtonCurrentFileCollectionType.ContextMenu = contextMenu;
        }

        private void filmsGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Environment.Exit(0); // remove after fixing, its just to ensure it always closes

            bool canClose = GetCurrentlyOpenedTabWorkingTextFile().TryToCloseFile();
            //todo replace with: check if any of

            e.Cancel = !canClose;
            SettingsManager.WindowLeft = Left;
            SettingsManager.WindowTop = Top;
            SettingsManager.WindowWidth = Width;
            SettingsManager.WindowHeight = Height;

            SettingsManager.SaveToConfFile();
        }

        private async void ManualCheckForUpdate(object sender, RoutedEventArgs e)
        {
            // Disable the button and update the UI
            CheckUpdatesButton.IsEnabled = false;
            SettingsUpdateBlock.Text = "Checking for update...";
            ImageNewVersionSettings.Visibility = Visibility.Collapsed;
            await UpdateVersionInformationAsync();
        }

        private void NewFileActionPrepareContextMenu()
        {
            ContextMenu contextMenu = new ContextMenu();

            foreach (CommonCollectionType commonCollection in CommonCollections.GetAllCollectionsAlphabetically())
            {
                MenuItem menuItem = new MenuItem
                {
                    Header = commonCollection.Name,
                    Icon = commonCollection.GetIconImage(),
                };

                menuItem.Click += (sender, e) =>
                {
                    TabsWorkingTextFiles.CreateEmptyWorkingFile(commonCollection);
                };
                contextMenu.Items.Add(menuItem);
            }

            ButtonNewFile.ContextMenu = contextMenu;
        }

        private void OpenContainingFolder(object sender, RoutedEventArgs e)
        {
            if (File.Exists(TabsWorkingTextFiles.CurrentlyOpenedWorkingFile().Filepath))
            {
                Process.Start("explorer.exe", "/select, " + TabsWorkingTextFiles.CurrentlyOpenedWorkingFile().Filepath);
            }
            else
            {
                Debug.WriteLine($"{TabsWorkingTextFiles.CurrentlyOpenedWorkingFile().Filepath} cannot be found");
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Open file";
            openFileDialog.Filter = "Text files (*.txt), (*.csv)|*.txt;*.csv";

            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            // todo instead of current directory, open the same path of most recent opened file

            if (openFileDialog.ShowDialog() == true)
            {
                TabsWorkingTextFiles.CreateNewWorkingFile(openFileDialog.FileName);
            }
        }

        private void ButtonOpenFileLocally_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Open file";
            openFileDialog.Filter = "Text files (*.txt), (*.csv)|*.txt;*.csv";

            string programDirectory = Environment.CurrentDirectory;
            string myDataDirectory = Path.Combine(programDirectory, "MyData");
            if (!Directory.Exists(myDataDirectory))
            {
                Directory.CreateDirectory(myDataDirectory);
            }

            openFileDialog.InitialDirectory = myDataDirectory;

            if (openFileDialog.ShowDialog() == true)
            {
                TabsWorkingTextFiles.CreateNewWorkingFile(openFileDialog.FileName);
            }
        }

        private void OpenLastOpenedFiles(string? newFilePath)
        {
            //   workingTextFile.OpenFilepathButSaveChangesFirst(newFilePath);
        }

        private void OpenLocalFolderButton_Click(object sender, RoutedEventArgs e)
        {
            LocalFilmsFilesService.OpenMyDataDirectoryFileExplorer();
        }

        private void RemoveColumnButton_Click(object sender, RoutedEventArgs e)
        {
            TabsWorkingTextFiles.CurrentlyOpenedWorkingFile().CollectionOfRecords.IdentifyColumnForDeletion();
        }

        private void RenameColumnButton_Click(object sender, RoutedEventArgs e)
        {
            TabsWorkingTextFiles.CurrentlyOpenedWorkingFile().CollectionOfRecords.RenameColumn();
        }

        private void ResetColumnsWidthAndOrder(object sender, RoutedEventArgs e)
        {
            GetCurrentlyOpenedTabWorkingTextFile().CollectionOfRecords.DataGridManager.ResetToDefault();
        }

        private void RevertChangesButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(GetCurrentlyOpenedTabWorkingTextFile().Filepath))
            {
                GetCurrentlyOpenedTabWorkingTextFile().CollectionOfRecords.ObservableCollectionOfRecords.Clear();
            }
            else
                OpenLastOpenedFiles(GetCurrentlyOpenedTabWorkingTextFile().Filepath);
            searchManager.SearchFilms();
        }

        private void SaveAllButton_Click(object sender, RoutedEventArgs e)
        {
            // iterate all tabs and save all of them
        }

        private void SaveAs(object sender, RoutedEventArgs e)
        {
            GetCurrentlyOpenedTabWorkingTextFile().SaveAs();
        }

        private void SaveButtonAction(object sender, RoutedEventArgs e)
        {
            GetCurrentlyOpenedTabWorkingTextFile().Save();
        }

        private void SaveLocally(object sender, RoutedEventArgs e)
        {
            if (localFilmsFilesService.SaveFileInProgramDirectory()) ;
        }

        private void searchClearButton_ClearText(object sender, RoutedEventArgs e)
        {
            searchTextBox.Text = null;
            SearchTextBox_LostFocus(sender, e);
            searchManager?.SearchFilms();
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (searchTextBox.Text == "Film title, year, specific words")
            {
                searchTextBox.Text = string.Empty;
                // Reset to default font style when the user starts typing
                searchTextBox.Foreground = new SolidColorBrush(Colors.Black);
                searchTextBox.FontStyle = FontStyles.Normal;
                searchTextBox.FontFamily = new FontFamily("Segoe UI");
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchTextBox.Text))
            {
                searchTextBox.Text = "Film title, year, specific words";
                // Reapply the placeholder style
                searchTextBox.Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)); // Bleak color
                searchTextBox.FontStyle = FontStyles.Italic;
                searchTextBox.FontFamily = new FontFamily("Segoe UI");
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (clearSearchButton == null)
            {
                return;
            }
            if (searchTextBox.Text != SearchBoxDefaultText && searchTextBox.Text.Length > 0)
            {
                clearSearchButton.Visibility = Visibility.Visible;
            }
            else
            {
                clearSearchButton.Visibility = Visibility.Hidden;
            }
            searchManager?.SearchFilms();

            return;
        }

        private void SelectLastButton(object sender, RoutedEventArgs e)

        {
            GetCurrentlyOpenedTabWorkingTextFile().ScrollToBottomOfList();
        }

        private void UpdateAverageFilmRating()
        {
            //if (filmsFileHandler.CollectionOfFilms.ListOfFilms.Count == 0)
            //{
            //    averageRatingLabel.Content = "No data";
            //}
            //else
            //{
            //    double averageRating = filmsFileHandler.StatisticsManager.GetAverageFilmRating();
            //    averageRatingLabel.Content = StatisticsManager.FormattedRating(averageRating);
            //}
        }

        private void UpdateButtons()
        {
            ButtonStateManager.UpdateAllButton(GetCurrentlyOpenedTabWorkingTextFile());
        }

        private void UpdateCommonCollectionElements(object? sender, WorkingTextFile.CommonCollectionTypeChangedEventArgs e)
        {
            TextBlockCurrentFileCollectionType.Text = GetCurrentlyOpenedTabWorkingTextFile().CommonCollectionType.Name;
            ImageCurrentFileCollectionType.Source = GetCurrentlyOpenedTabWorkingTextFile().CommonCollectionType.GetIconImageSource();
        }

        private void UpdateFileInformation()
        {
            TextBoxFilePath.Text = GetCurrentlyOpenedTabWorkingTextFile().Filepath;
            LabelCurrentDelimiter.Content = "[tab]";
        }

        private void UpdateStageTitle(object? sender, EventArgs e)
        {
            UpdateStageTitle();
        }

        //private async Task UpdateReportDecadalStatistics()
        //{
        //    cancellationTokenSourceForDecadalStatistics?.Cancel();
        //    cancellationTokenSourceForDecadalStatistics = new CancellationTokenSource();

        //    try
        //    {
        //        ObservableCollection<DecadalStatistic> decadesOfFilms = await filmsFileHandler.StatisticsManager.GetDecadalReport(cancellationTokenSourceForDecadalStatistics.Token).ConfigureAwait(false);

        //        await Application.Current.Dispatcher.InvokeAsync(() =>
        //        {
        //            decadalGrid.ItemsSource = decadesOfFilms;
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.ToString());
        //    }
        //}

        //private async Task UpdateReportYearlyStatistics()
        //{
        //    cancellationTokenSourceForYearlyStatistics?.Cancel();
        //    cancellationTokenSourceForYearlyStatistics = new CancellationTokenSource();
        //    try
        //    {
        //        ObservableCollection<YearlyStatistic> yearsOfFilms = await filmsFileHandler.StatisticsManager.GetYearlyReport(cancellationTokenSourceForYearlyStatistics.Token).ConfigureAwait(false);
        //        await Application.Current.Dispatcher.InvokeAsync(() =>
        //        {
        //            yearlyGrid.ItemsSource = yearsOfFilms;
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.ToString());
        //    }
        //}

        private async Task UpdateVersionInformationAsync()
        {
            UpdateStatusCheck isNewVersionAvailable;
            try
            {
                isNewVersionAvailable = await CheckForUpdateService.IsNewerVersionOnGitHubAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error has occured: {ex.Message}");
                isNewVersionAvailable = UpdateStatusCheck.ErrorChecking;
            }
            await Task.Delay(TimeSpan.FromSeconds(2)); // imitate long checking, just in case

            if (isNewVersionAvailable == UpdateStatusCheck.UpdateAvailable)
            {
                // Apply colour and display icon
                ImageNewVersion.Visibility = System.Windows.Visibility.Visible;
                RadialGradientBrush updateBrush = new RadialGradientBrush();
                updateBrush.GradientOrigin = new Point(0.5, 0.5);
                updateBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.5));
                updateBrush.GradientStops.Add(new GradientStop(Colors.Gold, 1.0));
                PanelVersion.Background = updateBrush;

                // Set hyperlink to open release page
                string uri = "https://github.com/OskarKamil/WatchedFilmsWPF/releases/tag/" + CheckForUpdateService.NewVersion;
                TextBlockNewVersion.Visibility = System.Windows.Visibility.Visible;
                Hyperlink releasesPage = new Hyperlink() { NavigateUri = new Uri(uri) };
                releasesPage.RequestNavigate += (sender, e) =>
                {
                    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                };
                releasesPage.Inlines.Add($"{CheckForUpdateService.NewVersion} is available. Click here to open download page.");

                TextBlockNewVersion.Inlines.Add(releasesPage);

                CheckUpdatesButton.IsEnabled = true;
                CheckUpdatesButton.Content = "Download page";
                SettingsUpdateBlock.Text = "Update found.";
                ImageNewVersionSettings.Visibility = Visibility.Visible;
                CheckUpdatesButton.Click -= ManualCheckForUpdate;
                CheckUpdatesButton.Click += (sender, e) =>
                {
                    Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                };
            }
            else if (isNewVersionAvailable == UpdateStatusCheck.NoUpdate)
            {
                CheckUpdatesButton.IsEnabled = true;
                ImageNewVersionSettings.Visibility = Visibility.Collapsed;
                SettingsUpdateBlock.Text = "No update found.";
            }
            else if (isNewVersionAvailable == UpdateStatusCheck.ErrorChecking)
            {
                CheckUpdatesButton.IsEnabled = true; SettingsUpdateBlock.Text = "Error checking.";
            }
            return;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.D) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                Width = 1200;
                Height = 600;
            }
        }

        private void CheckBoxReopenFilesLastSession(object sender, RoutedEventArgs e)
        {
        }
    }
}