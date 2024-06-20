using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.Models;
using WatchedFilmsTracker.Source.Services;
using WatchedFilmsTracker.Source.Views;
using static WatchedFilmsTracker.Source.Services.CheckForUpdateService;

namespace WatchedFilmsTracker
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationTokenSourceForDecadalStatistics;
        private CancellationTokenSource cancellationTokenSourceForYearlyStatistics;
        private DecadalStatisticsTableManager decadalStatisticsTableManager;
        private FilmsTableColumnManager filmsColumnsManager;
        private RecordManager filmsFile;
        private ObservableCollection<FilmRecord> filmsObservableList = new ObservableCollection<FilmRecord>();
        private StatisticsManager statisticsManager;
        private YearlyStatisticsTableManager yearlyStatisticsTableManager;

        public MainWindow()
        {
            //General GUI adjustments
            InitializeComponent(); // must be first line always
            LabelAuthor.Content = ProgramInformation.COPYRIGHT;
            LabelVersion.Content = ProgramInformation.VERSION;
            Closing += MainWindow_Closing; // override closing window

            //PROGRAM STATE MANAGER
            ProgramStateManager programStateManager = new ProgramStateManager(this);

            //BUTTON MANAGER
            List<Button> alwaysActiveButtons = new List<Button>();
            alwaysActiveButtons.Add(buttonNewFile);
            alwaysActiveButtons.Add(buttonOpenFile);
            alwaysActiveButtons.Add(buttonAbout);
            alwaysActiveButtons.Add(buttonSaveAs);

            List<Button> unsavedChangeButtons = new List<Button>();
            unsavedChangeButtons.Add(buttonSave);

            List<Button> openedFileButtons = new List<Button>();
            openedFileButtons.Add(buttonNewFilmRecord);
            openedFileButtons.Add(buttonClearAll);

            List<Button> atLeastOneRecordButtons = new List<Button>();
            atLeastOneRecordButtons.Add(buttonSelectLast);

            List<Button> selectedCellsButtons = new List<Button>();
            selectedCellsButtons.Add(buttonDeleteFilmRecord);

            List<Button> anyChangeButtons = new List<Button>();
            anyChangeButtons.Add(buttonRevertChanges);

            ButtonManager.AlwaysActiveButtons = (alwaysActiveButtons);
            ButtonManager.UnsavedChangeButtons = (unsavedChangeButtons);
            ButtonManager.OpenedFileButtons = (openedFileButtons);
            ButtonManager.SelectedCellsButtons = (selectedCellsButtons);
            ButtonManager.AnyChangeButtons = (anyChangeButtons);
            ButtonManager.AtLeastOneRecordButtons = (atLeastOneRecordButtons);
            //   buttonManager.TestButtons(true); // TESTING

            //SNAPSHOT SERVICE
            FileChangesSnapshotService.CreateSnapshotFolderIfNotExist();
            FileChangesSnapshotService.SubscribeToSaveCompletedEvent(this);

            //LOCAL MYFILMS FILE SERVICE
            LocalFilmsFilesService.CreateMyDataFolderIfNotExist();

            //SETTINGS
            ApplyUserSettingsToTheProgram(); // window size, position, last path, other settings

            //CHECK UPDATE ON STARTUP
            if (SettingsManager.CheckUpdateOnStartup)
                ManualCheckForUpdate(CheckUpdatesButton, null);

            //STATISTICS DISPLAY COLUMNS
            decadalStatisticsTableManager = new DecadalStatisticsTableManager(decadalGrid);
            yearlyStatisticsTableManager = new YearlyStatisticsTableManager(yearlyGrid);

            //FILMS TABLEVIEW DISPLAY VALUES
            filmsColumnsManager = new FilmsTableColumnManager(filmsGrid); // constructor builds columns and binds values

            filmsFile = OpenFilepath(SettingsManager.LastPath);
            AfterFileHasBeenLoaded();

            //GRIDLIST SELECTED LISTENER
            ProgramStateManager.IsSelectedCells = false;
            filmsGrid.SelectionChanged += (obs, args) =>
            {
                // A cell is selected
                // No cell is selected
                ProgramStateManager.IsSelectedCells = filmsGrid.SelectedItem != null;
            };
        }

        public event EventHandler FileOpened;

        public event EventHandler<RecordManager> SavedComplete;

        public void AfterFileHasBeenLoaded()
        {
            filmsObservableList = filmsFile.ListOfFilms;
            filmsGrid.ItemsSource = filmsObservableList;

            // Subscribe to PropertyChanged event of each FilmRecord instance
            filmsObservableList.CollectionChanged += filmsListHasChanged;

            foreach (var filmRecord in filmsObservableList)
            {
                filmRecord.PropertyChanged += FilmRecord_PropertyChanged;
            }

            SettingsManager.LastPath = (filmsFile.FilePath);
            ProgramStateManager.IsUnsavedChange = (false);
            ProgramStateManager.IsAnyChange = (false);
            ProgramStateManager.AtLeastOneRecord = filmsObservableList.Count > 0;
            statisticsManager = new StatisticsManager(filmsObservableList);
            UpdateStageTitle();
            filmsFile.CloseReader();

            if (SettingsManager.ScrollLastPosition)
                ScrollToBottomOfList();

            _ = UpdateStatistics();
        }

        public bool CloseFileAndAskToSave()
        {
            Debug.WriteLine("Stop with shutdown");
            if (ProgramStateManager.IsUnsavedChange)
            {
                Debug.WriteLine(filmsFile.FilePath);
                if (filmsFile.FilePath == "New File" || !SettingsManager.AutoSave)
                {
                    return ShowSaveChangesDialog();
                }
                else if (SettingsManager.AutoSave)
                {
                    Save();
                }
            }
            return !ProgramStateManager.IsUnsavedChange;
        }

        public bool ShowSaveChangesDialog()
        {
            SaveChangesDialog dialog = new SaveChangesDialog();
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();

            switch (dialog.Result)
            {
                case SaveChangesDialog.CustomDialogResult.Save:
                    return Save();

                case SaveChangesDialog.CustomDialogResult.NotSave:
                    return true;

                case SaveChangesDialog.CustomDialogResult.Cancel:
                default:
                    return false;
            }
        }

        public void UpdateNumberOfFilms()
        {
            filmsTotalLabel.Content = statisticsManager.GetNumberOfTotalWatchedFilms().ToString();
        }

        public void UpdateStageTitle()
        {
            string stageTitle = "";

            if (ProgramStateManager.IsUnsavedChange)
            {
                stageTitle = "*";
            }

            if (filmsFile is null || string.IsNullOrEmpty(filmsFile.FilePath))
                stageTitle += "New File" + " - " + ProgramInformation.PROGRAM_NAME;
            else
                stageTitle += filmsFile.FilePath + " - " + ProgramInformation.PROGRAM_NAME;

            // Assuming `this` refers to the current window instance
            this.Title = stageTitle;
        }

        protected void OnSaveCompleted(RecordManager filmsFile)
        {
            SavedComplete?.Invoke(this, filmsFile);
        }

        private void AboutButton(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("About dialog opened");
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this; // Set the owner window to enable modal behavior
            aboutWindow.ShowDialog();
        }

        private void AnyChangeHappen()
        {
            ProgramStateManager.IsAnyChange = true;
            ProgramStateManager.IsUnsavedChange = true;
            _ = UpdateStatistics();
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
            filmsFile.ListOfFilms.Clear();
            AnyChangeHappen();
        }

        private void DeleteFilmRecord(object sender, RoutedEventArgs e)
        {
            FilmRecord selected = filmsGrid.SelectedItem as FilmRecord;
            if (selected != null)
            {
                int selectedIndex = filmsGrid.SelectedIndex;
                filmsFile.DeleteRecordFromList(selected);
                if (selectedIndex == filmsGrid.Items.Count)
                    filmsGrid.SelectedIndex = selectedIndex - 1;
                else
                {
                    filmsGrid.SelectedIndex = selectedIndex;
                }
                AnyChangeHappen();
            }
        }

        private void FilmRecord_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IdInList")
            {
                AnyChangeHappen();
            }
        }

        private void filmsListHasChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.Print("list changed listener");
            ProgramStateManager.AtLeastOneRecord = filmsObservableList.Count > 0;
            AnyChangeHappen();
        }

        private void FilmsListHasChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // New items added to the list
                foreach (var newItem in e.NewItems)
                {
                    if (newItem is FilmRecord newRecord)
                    {
                        // Subscribe to PropertyChanged event of the new FilmRecord instance
                        newRecord.PropertyChanged += FilmRecord_PropertyChanged;
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                // Items removed from the list
                foreach (var oldItem in e.OldItems)
                {
                    if (oldItem is FilmRecord oldRecord)
                    {
                        // Unsubscribe from PropertyChanged event of the removed FilmRecord instance
                        oldRecord.PropertyChanged -= FilmRecord_PropertyChanged;
                    }
                }
            }
        }

        private void LoadLocally(object sender, RoutedEventArgs e)
        {
            //todo
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Call the ShutDown method when the window is closing
            bool canClose = CloseFileAndAskToSave();

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
            filmsFile = OpenFilepath(null);
            AfterFileHasBeenLoaded();
        }

        private void NewFilmRecord(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("new film record");
            FilmRecord newRecord = new FilmRecord(filmsObservableList.Count + 1);
            newRecord.PropertyChanged += FilmRecord_PropertyChanged;
            filmsObservableList.Add(newRecord);
            filmsGrid.SelectedItem = newRecord;
            filmsGrid.ScrollIntoView(filmsGrid.SelectedItem);

            if (SettingsManager.DefaultDateIsToday)
            {
                string formattedString = DateTime.Now.ToString("dd/MM/yyyy");
                newRecord.WatchDate = (formattedString);
            }
            AnyChangeHappen();
        }

        private void OpenContainingFolder(object sender, RoutedEventArgs e)
        {
            //todo
        }

        private void OpenFileChooser(object sender, RoutedEventArgs e)
        {
            if (CloseFileAndAskToSave())
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Open file";
                openFileDialog.Filter = "Text files (*.txt), (*.csv)|*.txt;*.csv";

                if (string.IsNullOrEmpty(filmsFile.FilePath) || !(File.Exists(filmsFile.FilePath)))
                {
                    openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
                }
                else
                {
                    string parentDirectory = Directory.GetParent(filmsFile.FilePath)?.FullName;
                    if (!string.IsNullOrEmpty(parentDirectory))
                    {
                        openFileDialog.InitialDirectory = parentDirectory;
                    }
                }

                if (openFileDialog.ShowDialog() == true)
                {
                    filmsFile = OpenFilepath(openFileDialog.FileName);
                    AfterFileHasBeenLoaded();
                }
            }
        }

        private RecordManager OpenFilepath(string? newFilePath)
        {
            RecordManager recordManager;
            if (string.IsNullOrEmpty(newFilePath) || !(File.Exists(newFilePath)))
            {
                if (CloseFileAndAskToSave())
                {
                    recordManager = new RecordManager();
                    recordManager.StartReader();
                }
                else
                {
                    return filmsFile;
                }
            }
            else
            {
                recordManager = new RecordManager(newFilePath);
                recordManager.StartReader();
            }
            return recordManager;
        }

        private void OpenLocalFolder(object sender, RoutedEventArgs e)
        {
            LocalFilmsFilesService.OpenMyDataDirectory();
        }

        private void ResetColumnsWidthAndOrder(object sender, RoutedEventArgs e)
        {
            filmsColumnsManager.ResetToDefault();
        }

        private void RevertChanges(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(filmsFile.FilePath))
            {
                filmsFile.ListOfFilms.Clear();
            }
            else
                filmsFile = OpenFilepath(filmsFile.FilePath);
            AfterFileHasBeenLoaded();
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private bool Save()
        {
            string filePath = filmsFile.FilePath;
            bool saved = false;

            if (string.IsNullOrEmpty(filePath))
            {
                saved = SaveAs();
                return saved;
            }

            filmsFile.StartWriter(filePath);
            OnSaveCompleted(filmsFile);
            ProgramStateManager.IsUnsavedChange = (false);

            // Return true if saving was successful
            return true;
        }

        private void SaveAs(object sender, RoutedEventArgs e)
        {
            SaveAs();
        }

        private bool SaveAs()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save file as";
            saveFileDialog.Filter = "Text files (*.txt), (*.csv)|*.txt;*.csv";

            if (!string.IsNullOrEmpty(filmsFile.FilePath))
            {
                string parentDirectory = Directory.GetParent(filmsFile.FilePath)?.FullName;
                if (!string.IsNullOrEmpty(parentDirectory))
                {
                    saveFileDialog.InitialDirectory = parentDirectory;
                }
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                filmsFile.StartWriter(saveFileDialog.FileName);
                filmsFile = OpenFilepath(saveFileDialog.FileName);
                AfterFileHasBeenLoaded();
                return true;
            }
            return false;
        }

        private void SaveLocally(object sender, RoutedEventArgs e)
        {
            //todo
            LocalFilmsFilesService.SaveFileInProgramDirectory();
        }

        private void ScrollToBottomOfList()
        {
            if (filmsObservableList.Count > 0)
                filmsGrid.ScrollIntoView(filmsObservableList.ElementAt(filmsObservableList.Count - 1));
        }

        private void SelectLastButton(object sender, RoutedEventArgs e)

        {
            ScrollToBottomOfList();
        }

        private void UpdateAverageFilmRating()
        {
            if (filmsFile.ListOfFilms.Count == 0)
            {
                averageRatingLabel.Content = "No data";
            }
            else
            {
                double averageRating = statisticsManager.GetAverageFilmRating();
                averageRatingLabel.Content = StatisticsManager.FormattedRating(averageRating);
            }
        }

        private async Task UpdateReportDecadalStatistics()
        {
            cancellationTokenSourceForDecadalStatistics?.Cancel();
            cancellationTokenSourceForDecadalStatistics = new CancellationTokenSource();

            try
            {
                ObservableCollection<DecadalStatistic> decadesOfFilms = await statisticsManager.GetDecadalReport(cancellationTokenSourceForDecadalStatistics.Token).ConfigureAwait(false);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    decadalGrid.ItemsSource = decadesOfFilms;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private async Task UpdateReportYearlyStatistics()
        {
            cancellationTokenSourceForYearlyStatistics?.Cancel();
            cancellationTokenSourceForYearlyStatistics = new CancellationTokenSource();
            try
            {
                ObservableCollection<YearlyStatistic> yearsOfFilms = await statisticsManager.GetYearlyReport(cancellationTokenSourceForYearlyStatistics.Token).ConfigureAwait(false);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    yearlyGrid.ItemsSource = yearsOfFilms;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private async Task UpdateStatistics()
        {
            UpdateNumberOfFilms();
            UpdateAverageFilmRating();

            // Heavy tasks
            var decadalTask = Task.Run(() => UpdateReportDecadalStatistics());
            var yearlyTask = Task.Run(() => UpdateReportYearlyStatistics());

            try
            {
                // Await both tasks to complete
                await Task.WhenAll(decadalTask, yearlyTask);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while updating statistics: {ex}");
            }
        }

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