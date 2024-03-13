using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
            SizeToContent = SizeToContent.WidthAndHeight;
            LabelAuthor.Content = ProgramInformation.COPYRIGHT;
            LabelVersion.Content = ProgramInformation.VERSION;
            this.Title = ProgramInformation.PROGRAM_NAME;
            Closing += MainWindow_Closing;

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

            //TABLEVIEW DISPLAY VALUES
            //titleColumn.DisplayMemberBinding = new Binding("EnglishTitle");
            //originalTitleColumn.DisplayMemberBinding = new Binding("OriginalTitle");
            //typeColumn.DisplayMemberBinding = new Binding("Type");
            //releaseYearColumn.DisplayMemberBinding = new Binding("ReleaseYear");
            //ratingColumn.DisplayMemberBinding = new Binding("Rating");
            //watchDateColumn.DisplayMemberBinding = new Binding("WatchDate");
            //commentsColumn.DisplayMemberBinding = new Binding("Comments");
            //idColumn.DisplayMemberBinding = new Binding("IdInList");
            OpenFilepath(SettingsManager.LastPath);

            filmsGrid.ItemsSource = filmsObservableList;

            //TABLEVIEW EDIT OPTIONS
            //titleColumn.CellTemplate = (DataTemplate)Resources["textFieldTemplate"];
            //originalTitleColumn.CellTemplate = (DataTemplate)Resources["textFieldTemplate"];
            //typeColumn.CellTemplate = (DataTemplate)Resources["textFieldTemplate"];
            //releaseYearColumn.CellTemplate = (DataTemplate)Resources["textFieldTemplate"];
            //ratingColumn.CellTemplate = (DataTemplate)Resources["textFieldTemplate"];
            //watchDateColumn.CellTemplate = (DataTemplate)Resources["textFieldTemplate"];
            //commentsColumn.CellTemplate = (DataTemplate)Resources["textFieldTemplate"];
            //filmsTable.Items.Refresh();

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

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            // Call the ShutDown method when the window is closing
            bool canClose = Shutdown();

            // Cancel the closing event if necessary
            e.Cancel = !canClose;
        }

        private void AboutButton(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("About dialog opened");
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
                Console.WriteLine("Here3");
                filmsFile = new RecordManager();
                Console.WriteLine("Here4");
                filmsFile.StartReader("New File");
                filePath = SettingsManager.LastPath;
                filmsObservableList = filmsFile.ListOfFilms;
                filmsGrid.ItemsSource = filmsObservableList;
                filePath = "New File";
                ProgramStateManager.IsAnyChange = (false);
                ProgramStateManager.IsUnsavedChange = (false);
                Console.WriteLine("New file created and names: " + filePath);
                SettingsManager.LastPath = (filePath);
                UpdateStatistics();
                UpdateStageTitle();
            }

        }

        private void OpenFilepath(string newFilePath)
        {
            Console.WriteLine("Trying to open new file");
            if (string.IsNullOrEmpty(newFilePath))
            {
                Console.WriteLine("Trying to create new file");
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
                Console.WriteLine("New file");
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

        public bool Shutdown()
        {

            Console.WriteLine("Stop with shutdown");
            if (SettingsManager.AutoSave && ProgramStateManager.IsUnsavedChange)
            {
                Save();
            }
            else
            {
                Console.WriteLine("Here");

                if (ProgramStateManager.IsUnsavedChange)
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
                        Console.WriteLine("Option Cancel");
                        return false;
                    }

                }
                Console.WriteLine("Here2");
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

            Console.WriteLine("Updating stage title to: " + filePath);
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
                filmsFile.DeleteRecordFromList(selected);
                int selectedIndex = filmsGrid.SelectedIndex;
                if (selectedIndex != -1 && selectedIndex != 0)
                    filmsGrid.SelectedIndex = selectedIndex + 1;
                ProgramStateManager.IsAnyChange = (true);
                ProgramStateManager.IsUnsavedChange = (true);
            }
            filmsGrid.Items.Refresh();
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
            ProgramStateManager.IsAnyChange = true;
        }
    }
}