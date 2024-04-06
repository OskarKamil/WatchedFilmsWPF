using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker
{
    public partial class MainWindow : Window
    {
        private ColumnsManager columnsManager;
        private string filePath;
        private RecordManager filmsFile;
        private ObservableCollection<FilmRecord> filmsObservableList = new ObservableCollection<FilmRecord>();
        private StatisticsManager statisticsManager;

        public MainWindow()
        {
            //General GUI adjustments
            InitializeComponent();
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

            //SETTINGS
            SettingsManager.ReadSettingsFile();
            autosaveBox.IsChecked = SettingsManager.AutoSave;
            defaultDateBox.IsChecked = SettingsManager.DefaultDateIsToday;
            this.Left = SettingsManager.WindowLeft;
            this.Top = SettingsManager.WindowTop;
            this.Width = SettingsManager.WindowWidth;
            this.Height = SettingsManager.WindowHeight;

            //TABLEVIEW DISPLAY VALUES
            DataGridTextColumn id = new DataGridTextColumn();
            id.Header = "#";
            id.Binding = new Binding("IdInList");
            filmsGrid.Columns.Add(id);
            id.IsReadOnly = true;
            //  id.Width = 0;

            DataGridTextColumn englishTitle = new DataGridTextColumn();
            englishTitle.Header = "English title";
            englishTitle.Binding = new Binding("EnglishTitle");
            filmsGrid.Columns.Add(englishTitle);
            // englishTitle.Width = 0;

            DataGridTextColumn originalTitle = new DataGridTextColumn();
            originalTitle.Header = "Original title";
            originalTitle.Binding = new Binding("OriginalTitle");
            filmsGrid.Columns.Add(originalTitle);
            // originalTitle.Width = 50;

            DataGridTextColumn type = new DataGridTextColumn();
            type.Header = "Type";
            type.Binding = new Binding("Type");
            filmsGrid.Columns.Add(type);
            // type.Width = 50;

            DataGridTextColumn releaseYear = new DataGridTextColumn();
            releaseYear.Header = "Release year";
            releaseYear.Binding = new Binding("ReleaseYear");
            filmsGrid.Columns.Add(releaseYear);
            // releaseYear.Width = 50;

            DataGridTextColumn rating = new DataGridTextColumn();
            rating.Header = "Rating";
            rating.Binding = new Binding("Rating");
            filmsGrid.Columns.Add(rating);
            //   rating.Width = 50;

            DataGridTextColumn watchDate = new DataGridTextColumn();
            watchDate.Header = "Watch date";
            watchDate.Binding = new Binding("WatchDate");
            filmsGrid.Columns.Add(watchDate);
            //   watchDate.Width = 50;

            DataGridTextColumn comments = new DataGridTextColumn();
            comments.Header = "Comments";
            comments.Binding = new Binding("Comments");
            filmsGrid.Columns.Add(comments);
            comments.Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            columnsManager = new ColumnsManager(filmsGrid);

            OpenFilepath(SettingsManager.LastPath);
            filmsGrid.ItemsSource = filmsObservableList;
            filmsObservableList.CollectionChanged += filmsListHasChanged;

            // Subscribe to PropertyChanged event of each FilmRecord instance
            foreach (var filmRecord in filmsObservableList)
            {
                filmRecord.PropertyChanged += FilmRecord_PropertyChanged;
            }

            //GRIDLIST SELECTED LISTENER
            ProgramStateManager.IsSelectedCells = false;
            filmsGrid.SelectionChanged += (obs, args) =>
            {
                // A cell is selected
                // No cell is selected
                ProgramStateManager.IsSelectedCells = filmsGrid.SelectedItem != null;
            };
        }

        public void AfterFileHasBeenLoaded()
        {
            filmsObservableList = filmsFile.ListOfFilms;
            filmsGrid.ItemsSource = filmsObservableList;
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
            MessageBoxResult result = MessageBox.Show("Save changes in this file?", "Save changes", MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.Yes)
            {
                bool saved = Save();
                return saved;
            }
            else if (result == MessageBoxResult.No)
            {
                return true;
            }
            else if (result == MessageBoxResult.Cancel)
            {
                Debug.WriteLine("Option Cancel");
                return false;
            }
            else
            {
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

        private void BuildDynamicStatistics()
        {
            UpdateDecadesOfFilms();
            UpdateAverageYearlyReport();
        }

        private void CheckBoxAutoSave(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            SettingsManager.AutoSave = (bool)checkBox.IsChecked; ;
            checkBox.IsChecked = SettingsManager.AutoSave;
        }

        private void CheckBoxDefaultDate(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            SettingsManager.DefaultDateIsToday = (bool)checkBox.IsChecked; ;
            checkBox.IsChecked = SettingsManager.DefaultDateIsToday;
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
            AnyChangeHappen();
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
            columnsManager.ResetToDefault();
            filmsGrid.Items.Refresh();
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

        private void tester(object sender, RoutedEventArgs e)
        {
            // statisticsManager.GetAllDecadesStatistics();
        }

        private void UpdateAverageFilmPerDay()
        {
            //averageFilmPerDayLabel.Content = statisticsManager.GetAverageWatchStatistics();
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
                averageRatingLabel.Content = statisticsManager.FormattedRating(averageRating);
            }
        }

        private void UpdateAverageYearlyReport()
        {
            //throw new NotImplementedException();
        }

        private void UpdateDecadesOfFilms()
        {
            Dictionary<int, List<FilmRecord>> decades = statisticsManager.GetAllDecadesStatistics();

            // Clear all old decades
            decadePanel.Children.Clear();
            totalFilmPanel.Children.Clear();
            averageRatingPanel.Children.Clear();

            //GroupBox decadalBox = new GroupBox();
            //MainPanelDecadal

            Label decadeL = new Label();
            decadeL.Content = "Decade";
            Label averageRating = new Label();
            averageRating.Content = "Average rating";
            Label totalFilms = new Label();
            totalFilms.Content = "Total films";
            decadePanel.Children.Add(decadeL);
            totalFilmPanel.Children.Add(totalFilms);
            averageRatingPanel.Children.Add(averageRating);

            foreach (var decadeGroup in decades)
            {
                int decade = decadeGroup.Key;
                Collection<FilmRecord> filmsInDecade = new Collection<FilmRecord>(decadeGroup.Value);

                Label decadeLabel = new Label();
                decadeLabel.Content = decade + "s";
                Label averageRatingLabel = new Label();
                averageRatingLabel.Content = statisticsManager.FormattedRating(statisticsManager.GetAverageFilmRating(filmsInDecade));
                Label totalFilmsLabel = new Label();
                totalFilmsLabel.Content = statisticsManager.GetNumberOfTotalWatchedFilms(filmsInDecade);
                decadePanel.Children.Add(decadeLabel);
                averageRatingPanel.Children.Add(averageRatingLabel);
                totalFilmPanel.Children.Add(totalFilmsLabel);
            }
        }

        private void UpdateStatistics()
        {
            UpdateNumberOfFilms();
            UpdateAverageFilmRating();
            UpdateAverageFilmPerDay();
            UpdateDecadesOfFilms();
            UpdateAverageYearlyReport();
        }
    }
}