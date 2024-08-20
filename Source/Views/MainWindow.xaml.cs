using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.ManagingFilmsFile;
using WatchedFilmsTracker.Source.Models;
using WatchedFilmsTracker.Source.Services;
using static WatchedFilmsTracker.Source.Services.CheckForUpdateService;

namespace WatchedFilmsTracker
{
    public partial class MainWindow : Window
    {
        private const string SearchBoxDefaultText = "Film title, year, specific words";
        private CancellationTokenSource cancellationTokenSourceForDecadalStatistics;
        private CancellationTokenSource cancellationTokenSourceForYearlyStatistics;
        private DecadalStatisticsTableManager decadalStatisticsTableManager;
        private DataGridManager filmsColumnsManager;
        private WorkingTextFile workingTextFile;
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

            //PROGRAM STATE MANAGER
            ProgramStateManager programStateManager = new ProgramStateManager(this);

            //BUTTON MANAGER
            ButtonManager.AlwaysActiveButtons.Add(buttonOpenFile);
            ButtonManager.AlwaysActiveButtons.Add(buttonAbout);
            ButtonManager.AlwaysActiveButtons.Add(buttonSaveAs);
            ButtonManager.AlwaysActiveButtons.Add(buttonNewFile);
            ButtonManager.AlwaysActiveButtons.Add(buttonOpenLocally);

            ButtonManager.UnsavedChangeButtons.Add(buttonSave);

            ButtonManager.OpenedFileButtons.Add(buttonNewFilmRecord);
            ButtonManager.OpenedFileButtons.Add(buttonClearAll);

            ButtonManager.AtLeastOneRecordButtons.Add(buttonSelectLast);

            ButtonManager.SelectedCellsButtons.Add(buttonDeleteFilmRecord);

            ButtonManager.AnyChangeButtons.Add(buttonRevertChanges);

            ButtonManager.FileExistsOnDiskButtons.Add(buttonOpenContainingFolder);

            ButtonManager.FileIsNotInLocalMyDataDirectoryButtons.Add(buttonSaveLocally);

            //SETTINGS
            SettingsManager.LoadDefaultSettings();
            SettingsManager.LoadSettingsFromConfigFile();
            ApplyUserSettingsToTheProgram(); // window size, position, last path, other settings

            //CHECK UPDATE ON STARTUP
            if (SettingsManager.CheckUpdateOnStartup)
                ManualCheckForUpdate(CheckUpdatesButton, null);

            //FILEMANAGER
            workingTextFile = new WorkingTextFile();
            workingTextFile.setUpFilmsDataGrid(dataGridMainWindow);
            workingTextFile.setUpMainWindow(this);
            workingTextFile.DeleteRecordAction = DeleteFilmRecord_ButtonClick;
            OpenFilepath(SettingsManager.LastPath);

            //LOCAL FILES SERVICE
            localFilmsFilesService = new LocalFilmsFilesService(workingTextFile);
            LocalFilmsFilesService.CreateMyDataFolderIfNotExist();

            //STATISTICS DISPLAY COLUMNS
            decadalStatisticsTableManager = new DecadalStatisticsTableManager(decadalGrid);
            yearlyStatisticsTableManager = new YearlyStatisticsTableManager(yearlyGrid);

            //SNAPSHOT SERVICE
            FileChangesSnapshotService.CreateSnapshotFolderIfNotExist();
            FileChangesSnapshotService.FileManager = workingTextFile;
            FileChangesSnapshotService.SubscribeToSaveCompletedEvent(this);

            //SEARCH MANAGER
            searchManager = new SearchManager(workingTextFile, searchTextBox, dataGridMainWindow);

            //GRIDLIST SELECTED LISTENER
            ProgramStateManager.IsSelectedCells = false;
            dataGridMainWindow.SelectedCellsChanged += (obs, args) =>
            {
                Debug.WriteLine("Selected cells changes, or deselected if filtered");
                ProgramStateManager.IsSelectedCells = dataGridMainWindow.SelectedCells.Count > 0;
            };
        }

        public event EventHandler FileOpened;

        public void DeleteFilmRecord_ButtonClick(object sender, RoutedEventArgs e) // RemoveFilmRecord, DeleteFilmRecord
        {
            if (dataGridMainWindow.SelectedCells.Count > 0)
            {
                RecordModel selected = dataGridMainWindow.SelectedCells[0].Item as RecordModel;
                //    filmsFileHandler.CollectionOfFilms.DeleteRecordFromList(selected);
            }
        }

        public void UpdateNumberOfFilms()
        {
            viewModel.TotalFilmsWatched = workingTextFile.StatisticsManager.GetNumberOfTotalWatchedFilms();
        }

        public void UpdateStageTitle()
        {
            string stageTitle = "";

            if (ProgramStateManager.IsUnsavedChange)
            {
                stageTitle = "*";
            }

            if (workingTextFile.CollectionOfFilms is null || string.IsNullOrEmpty(workingTextFile.CollectionOfFilms.FilePath))
                stageTitle += "New File" + " - " + ProgramInformation.PROGRAM_NAME;
            else
                stageTitle += workingTextFile.CollectionOfFilms.FilePath + " - " + ProgramInformation.PROGRAM_NAME;

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

        private void AddColumnButton(object sender, RoutedEventArgs e)
        {
        }

        private void ApplyUserSettingsToTheProgram()
        {
            autosaveBox.IsChecked = SettingsManager.AutoSave;
            defaultDateBox.IsChecked = SettingsManager.DefaultDateIsToday;
            updateStartUpBox.IsChecked = SettingsManager.CheckUpdateOnStartup;
            scrollLastPositionBox.IsChecked = SettingsManager.ScrollLastPosition;
            this.Left = SettingsManager.WindowLeft;
            this.Top = SettingsManager.WindowTop;
            this.Width = SettingsManager.WindowWidth;
            this.Height = SettingsManager.WindowHeight;
        }

        // not in use for now
        //private void BuildDynamicStatistics()
        //{
        //    UpdateReportDecadalStatistics();
        //    UpdateReportYearlyStatistics();
        //}

        private void CheckBoxAutoSave(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            SettingsManager.AutoSave = (bool)checkBox.IsChecked;
            checkBox.IsChecked = SettingsManager.AutoSave;
        }

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
            workingTextFile.CollectionOfFilms.DeleteAllRecords();
        }

        private void filmsGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Debug.WriteLine("table scroled");
            // todo
        }

        private void LoadLocally(object sender, RoutedEventArgs e)
        {
            workingTextFile.LoadLocally();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Call the ShutDown method when the window is closing
            bool canClose = workingTextFile.CloseFileAndAskToSave();

            // Cancel the closing event if necessary

            e.Cancel = !canClose;
            SettingsManager.WindowLeft = Left;
            SettingsManager.WindowTop = Top;
            SettingsManager.WindowWidth = Width;
            SettingsManager.WindowHeight = Height;
        }

        private async void ManualCheckForUpdate(object sender, RoutedEventArgs e)
        {
            // Disable the button and update the UI
            CheckUpdatesButton.IsEnabled = false;
            SettingsUpdateBlock.Text = "Checking for update...";
            ImageNewVersionSettings.Visibility = Visibility.Collapsed;
            await UpdateVersionInformationAsync();
        }

        private void NewFile(object sender, RoutedEventArgs e)
        {
            workingTextFile.NewFile();
        }

        private void NewFilmRecord_ButtonClick(object sender, RoutedEventArgs e) // AddFilmRecord, NewFilmRecord
        {
              workingTextFile.CollectionOfFilms.AddEmptyRecordToList();
        }

        private void OpenContainingFolder(object sender, RoutedEventArgs e)
        {
            if (File.Exists(workingTextFile.CollectionOfFilms.FilePath))
            {
                Process.Start("explorer.exe", "/select, " + workingTextFile.CollectionOfFilms.FilePath);
            }
            else
            {
                Debug.WriteLine($"{workingTextFile.CollectionOfFilms.FilePath} cannot be found");
            }
        }

        private void OpenFileChooser(object sender, RoutedEventArgs e)
        {
            if (workingTextFile.CloseFileAndAskToSave())
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Open file";
                openFileDialog.Filter = "Text files (*.txt), (*.csv)|*.txt;*.csv";

                if (string.IsNullOrEmpty(workingTextFile.CollectionOfFilms.FilePath) || !(File.Exists(workingTextFile.CollectionOfFilms.FilePath)))
                {
                    openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
                }
                else
                {
                    string parentDirectory = Directory.GetParent(workingTextFile.CollectionOfFilms.FilePath)?.FullName;
                    if (!string.IsNullOrEmpty(parentDirectory))
                    {
                        openFileDialog.InitialDirectory = parentDirectory;
                    }
                }

                if (openFileDialog.ShowDialog() == true)
                {
                    OpenFilepath(openFileDialog.FileName);
                }
            }
        }

        private void OpenFilepath(string? newFilePath)
        {
            workingTextFile.OpenFilepathButSaveChangesFirst(newFilePath);
        }

        private void OpenLocalFolder(object sender, RoutedEventArgs e)
        {
            LocalFilmsFilesService.OpenMyDataDirectoryFileExplorer();
        }

        private void RemoveColumnButton(object sender, RoutedEventArgs e)
        {
        }

        private void ResetColumnsWidthAndOrder(object sender, RoutedEventArgs e)
        {
            filmsColumnsManager.ResetToDefault();
        }

        private void RevertChanges(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(workingTextFile.CollectionOfFilms.FilePath))
            {
                workingTextFile.CollectionOfFilms.ListOfFilms.Clear();
            }
            else
                OpenFilepath(workingTextFile.CollectionOfFilms.FilePath);
            searchManager.SearchFilms();
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            workingTextFile.Save();
        }

        private void SaveAs(object sender, RoutedEventArgs e)
        {
            workingTextFile.SaveAs();
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
            workingTextFile.ScrollToBottomOfList();
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
                string uri = "https://github.com/OskarKamil/WatchedFilmsWPF/releases/tag/" + CheckForUpdateService.NewVersionString;
                TextBlockNewVersion.Visibility = System.Windows.Visibility.Visible;
                Hyperlink releasesPage = new Hyperlink() { NavigateUri = new Uri(uri) };
                releasesPage.RequestNavigate += (sender, e) =>
                {
                    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                };
                releasesPage.Inlines.Add($"{CheckForUpdateService.NewVersionString} is available. Click here to open download page.");

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
            // Check if Control+D is pressed
            if (Keyboard.IsKeyDown(Key.D) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                // Set the window size to 1200x600
                Width = 1200;
                Height = 600;
            }
        }
    }
}