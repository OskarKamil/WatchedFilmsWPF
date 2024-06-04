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
using static WatchedFilmsTracker.Source.Services.NewestVersionChecker;

namespace WatchedFilmsTracker
{
    public partial class MainWindow : Window
    {
        private DecadalStatisticsTableManager decadalStatisticsTableManager;
        private string filePath;
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
            this.Title = ProgramInformation.PROGRAM_NAME;
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

            List<Button> selectedCellsButtons = new List<Button>();
            selectedCellsButtons.Add(buttonDeleteFilmRecord);

            List<Button> anyChangeButtons = new List<Button>();
            anyChangeButtons.Add(buttonRevertChanges);

            ButtonManager.SetAlwaysActiveButtons(alwaysActiveButtons);
            ButtonManager.SetUnsavedChangeButtons(unsavedChangeButtons);
            ButtonManager.SetOpenedFileButtons(openedFileButtons);
            ButtonManager.SetSelectedCellsButtons(selectedCellsButtons);
            ButtonManager.SetAnyChangeButtons(anyChangeButtons);
            //   buttonManager.TestButtons(true); // TESTING

            //SNAPSHOT SERVICE
            FileChangesSnapshotService.CreateSnapshotFolderIfNotExist();
            FileChangesSnapshotService.SubscribeToSaveCompletedEvent(this);

            //SETTINGS
            ApplyUserSettingsToTheProgram(); // window size, position, last path, other settings

            //Check update on startup
            if (SettingsManager.CheckUpdateOnStartup)
                ManualCheckForUpdate(CheckUpdatesButton, null);

            //STATISTICS DISPLAY COLUMNS
            decadalStatisticsTableManager = new DecadalStatisticsTableManager(decadalGrid);
            yearlyStatisticsTableManager = new YearlyStatisticsTableManager(yearlyGrid);

            //FILMS TABLEVIEW DISPLAY VALUES
            filmsColumnsManager = new FilmsTableColumnManager(filmsGrid); // constructor builds columns and binds values

            OpenFilepath(SettingsManager.LastPath);

            //GRIDLIST SELECTED LISTENER
            ProgramStateManager.IsSelectedCells = false;
            filmsGrid.SelectionChanged += (obs, args) =>
            {
                // A cell is selected
                // No cell is selected
                ProgramStateManager.IsSelectedCells = filmsGrid.SelectedItem != null;
            };
        }

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

            SettingsManager.LastPath = (filePath);
            ProgramStateManager.IsUnsavedChange = (false);
            ProgramStateManager.IsAnyChange = (false);
            statisticsManager = new StatisticsManager(filmsObservableList);
            UpdateStageTitle();
            filmsFile.CloseReader();

            UpdateStatistics();
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

            Debug.WriteLine("Updating stage title to: " + filePath);
            stageTitle += filePath + " - " + ProgramInformation.PROGRAM_NAME;

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
            UpdateStatistics();
        }

        private void ApplyUserSettingsToTheProgram()
        {
            autosaveBox.IsChecked = SettingsManager.AutoSave;
            defaultDateBox.IsChecked = SettingsManager.DefaultDateIsToday;
            updateStartUpBox.IsChecked = SettingsManager.CheckUpdateOnStartup;
            this.Left = SettingsManager.WindowLeft;
            this.Top = SettingsManager.WindowTop;
            this.Width = SettingsManager.WindowWidth;
            this.Height = SettingsManager.WindowHeight;
        }

        private void BuildDynamicStatistics()
        {
            UpdateDecadesOfFilmsStatistics();
            UpdateYearlyReportStatistics();
        }

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
            //filmsGrid.Items.Refresh();
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
            // Checking for update
            CheckUpdatesButton.IsEnabled = false;
            SettingsUpdateBlock.Text = "Checking for update...";
            ImageNewVersionSettings.Visibility = Visibility.Collapsed;
            await UpdateVersionInformationAsync();
        }

        private void NewFile(object sender, RoutedEventArgs e)
        {
            OpenFilepath(null);
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

        private void OpenFileChooser(object sender, RoutedEventArgs e)
        {
            if (CloseFileAndAskToSave())
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Open file";
                openFileDialog.Filter = "Text files (*.txt), (*.csv)|*.txt;*.csv";

                if ("New File".Equals(filePath))
                {
                    Debug.WriteLine("New file");
                    openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
                }
                else
                {
                    string parentDirectory = Directory.GetParent(filePath)?.FullName;
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
            Debug.WriteLine("Trying to open new file");
            if (string.IsNullOrEmpty(newFilePath))
            {
                Debug.WriteLine("Trying to create new file");
                if (CloseFileAndAskToSave())
                {
                    filmsFile = new RecordManager();
                    filmsFile.StartReader("New File");
                    filePath = "New File";
                }
                else
                {
                    return;
                }
            }
            else
            {
                filmsFile = new RecordManager();
                filePath = newFilePath;
                filmsFile.StartReader(filePath);
            }
            AfterFileHasBeenLoaded();
        }

        private void ResetColumnsWidthAndOrder(object sender, RoutedEventArgs e)
        {
            filmsColumnsManager.ResetToDefault();
        }

        private void RevertChanges(object sender, RoutedEventArgs e)
        {
            OpenFilepath(filePath);
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private bool Save()
        {
            string filePath = filmsFile.FilePath;
            bool saved = false;

            if (string.IsNullOrEmpty(filePath) || filePath.Equals("New File"))
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
                OpenFilepath(saveFileDialog.FileName);
                return true;
            }
            return false;
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

        private void UpdateDecadesOfFilmsStatistics()
        {
            ObservableCollection<DecadalStatistic> decadesOfFilms = statisticsManager.GetDecadalReport();
            decadalGrid.ItemsSource = decadesOfFilms;
        }

        private void UpdateStatistics()
        {
            UpdateNumberOfFilms();
            UpdateAverageFilmRating();
            UpdateDecadesOfFilmsStatistics();
            UpdateYearlyReportStatistics();
        }

        private async Task UpdateVersionInformationAsync()
        {
            UpdateStatusCheck isNewVersionAvailable = await NewestVersionChecker.IsNewerVersionOnGitHubAsync();
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
                string uri = "https://github.com/OskarKamil/WatchedFilmsWPF/releases/tag/" + NewestVersionChecker.NewVersionString;
                TextBlockNewVersion.Visibility = System.Windows.Visibility.Visible;
                Hyperlink releasesPage = new Hyperlink() { NavigateUri = new Uri(uri) };
                releasesPage.RequestNavigate += (sender, e) =>
                {
                    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                };
                releasesPage.Inlines.Add($"{NewestVersionChecker.NewVersionString} is available. Click here to open download page.");

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

        private void UpdateYearlyReportStatistics()
        {
            ObservableCollection<YearlyStatistic> yearsOfFilms = statisticsManager.GetYearlyReport();
            yearlyGrid.ItemsSource = yearsOfFilms;
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