using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Linq;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker
{
    public partial class MainWindow : Window
    {
        private RecordManager filmsFile;
        private string filePath;
        private ObservableCollection<FilmRecord> filmsObservableList = new ObservableCollection<FilmRecord>();

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


            OpenFilepath(SettingsManager.LastPath);
            filmsGrid.ItemsSource = filmsObservableList;


            //TABLEVIEW SELECTED LISTENER
            ProgramStateManager.IsSelectedCells = false;
            filmsGrid.SelectionChanged += (obs, args) =>
            {
                // A cell is selected
                // No cell is selected
                ProgramStateManager.IsSelectedCells = filmsGrid.SelectedItem != null;
            };

            //STATISTICS
            UpdateStatistics();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Call the ShutDown method when the window is closing
            bool canClose = Shutdown();

            // Cancel the closing event if necessary

            e.Cancel = !canClose;
            SettingsManager.WindowLeft = Left;
            SettingsManager.WindowTop = Top;
            SettingsManager.WindowWidth = Width;
            SettingsManager.WindowHeight = Height;
        }

        private void AboutButton(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("About dialog opened");
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this; // Set the owner window to enable modal behavior
            aboutWindow.ShowDialog();
        }

        private void NewFile(object sender, RoutedEventArgs e)
        {
            NewFile();

        }

        private void NewFile()
        {
            // this program can open only one file at a time, in the future there will be tabs
            // for now, opening new file will trigger a close action for the current file
            // and will try to save current file before creating a new file
            if (Shutdown())
            {
                filmsFile = new RecordManager();
                filmsFile.StartReader("New File");
                filePath = SettingsManager.LastPath;
                filmsObservableList = filmsFile.ListOfFilms;
                filmsGrid.ItemsSource = filmsObservableList;
                filePath = "New File";
                ProgramStateManager.IsAnyChange = (false);
                ProgramStateManager.IsUnsavedChange = (false);
                Debug.WriteLine("New file created and names: " + filePath);
                SettingsManager.LastPath = (filePath);
                UpdateStatistics();
                UpdateStageTitle();
            }

        }

        private void OpenFilepath(string newFilePath)
        {
            Debug.WriteLine("Trying to open new file");
            if (string.IsNullOrEmpty(newFilePath))
            {
                Debug.WriteLine("Trying to create new file");
                NewFile();
            }
            else
            {
                filePath = newFilePath;
                filmsFile = new RecordManager();
                filmsFile.StartReader(filePath);
                filmsObservableList = filmsFile.ListOfFilms;
                filmsGrid.ItemsSource = filmsObservableList;
                SettingsManager.LastPath = (filePath);
                ProgramStateManager.IsUnsavedChange = (false);
                ProgramStateManager.IsAnyChange = (false);
                UpdateStatistics();
                filmsFile.CloseReader(); // close file after reading it, so you can safely write to it without locking
                // UpdateStageTitle();
            }
        }

        private void OpenFileChooser(object sender, RoutedEventArgs e)
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

        public bool Shutdown()
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

        private void UpdateAverageFilmPerDay()
        {
            averageFilmPerDayLabel.Content = filmsFile.GetAverageWatchStatistics();
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

        public void UpdateNumberOfFilms()
        {
            filmsTotalLabel.Content = filmsFile.GetNumberOfTotalWatchedFilms().ToString();
        }

        private void UpdateAverageFilmRating()
        {
            if (filmsFile.ListOfFilms.Count == 0)
            {
                averageRatingLabel.Content = "No data";
            }
            else
            {
                double averageRating = filmsFile.GetAverageFilmRating();
                string formattedRating = averageRating.ToString("#.##") + "/4";
                averageRatingLabel.Content = formattedRating;
            }
        }

        private void RevertChanges(object sender, RoutedEventArgs e)
        {
            OpenFilepath(filePath);
        }

        private void NewFilmRecord(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("new film record");
            FilmRecord newRecord = new FilmRecord(filmsObservableList.Count + 1);
            filmsObservableList.Add(newRecord);
            filmsGrid.SelectedItem = newRecord;

            if (SettingsManager.DefaultDateIsToday)
            {
                string formattedString = DateTime.Now.ToString("dd/MM/yyyy");
                newRecord.WatchDate = (formattedString);
            }
            ProgramStateManager.IsAnyChange = (true);
            ProgramStateManager.IsUnsavedChange = (true);

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
                ProgramStateManager.IsAnyChange = (true);
                ProgramStateManager.IsUnsavedChange = (true);
            }
            //filmsGrid.Items.Refresh();
        }

        private void ClearAll(object sender, RoutedEventArgs e)
        {
            filmsFile.ListOfFilms.Clear();
            ProgramStateManager.IsAnyChange = (true);
            ProgramStateManager.IsUnsavedChange = (true);

        }

        private void UpdateStatistics()
        {
            UpdateNumberOfFilms();
            UpdateAverageFilmRating();
            UpdateAverageFilmPerDay();
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

        private void FilmGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Debug.WriteLine("Is any change shoulb be invoked");
            ProgramStateManager.IsAnyChange = true;
            ProgramStateManager.IsUnsavedChange = true;
        }
    }
}