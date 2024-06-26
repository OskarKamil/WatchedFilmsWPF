using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.Models;
using WatchedFilmsTracker.Source.Views;

namespace WatchedFilmsTracker.Source.Managers
{
    internal class FileManager
    {
        private RecordManager _filmsFile;
        private ObservableCollection<FilmRecord> filmsObservableList;
        private StatisticsManager statisticsManager;
        private DataGrid visualFilmsTable;
        private MainWindow window;

        public FileManager()
        {
            FilmsObservableList = new ObservableCollection<FilmRecord>();
        }

        public event EventHandler<RecordManager> SavedComplete;

        public RecordManager FilmsFile { get => _filmsFile; set => _filmsFile = value; }

        public ObservableCollection<FilmRecord> FilmsObservableList { get => filmsObservableList; set => filmsObservableList = value; }

        public void AfterFileHasBeenLoaded() //todo try to move this to filemanager
        {
            FilmsObservableList = FilmsFile.ListOfFilms;
            visualFilmsTable.ItemsSource = FilmsObservableList;

            // Subscribe to PropertyChanged event of each FilmRecord instance
            FilmsObservableList.CollectionChanged += filmsListHasChanged;

            foreach (var filmRecord in FilmsObservableList)
            {
                filmRecord.PropertyChanged += FilmRecord_PropertyChanged;
            }

            SettingsManager.LastPath = (FilmsFile.FilePath);
            ProgramStateManager.IsUnsavedChange = (false);
            ProgramStateManager.IsAnyChange = (false);
            ProgramStateManager.AtLeastOneRecord = FilmsObservableList.Count > 0;

            if (string.IsNullOrEmpty(FilmsFile.FilePath))
            {
                ProgramStateManager.IsFileSavedOnDisk = false;
                ProgramStateManager.IsFileInLocalMyDataFolder = false;
            }
            else
            {
                ProgramStateManager.IsFileSavedOnDisk = true;

                string lastDirectory = Directory.GetParent(FilmsFile.FilePath).Name;
                Debug.WriteLine($"{lastDirectory} is folder where the file is in");
                if (lastDirectory == "MyData")
                    ProgramStateManager.IsFileInLocalMyDataFolder = true;
                else
                    ProgramStateManager.IsFileInLocalMyDataFolder = false;
            }

            statisticsManager = new StatisticsManager(FilmsObservableList);
            window.UpdateStageTitle();
            FilmsFile.CloseReader();

            if (SettingsManager.ScrollLastPosition)
                ScrollToBottomOfList();

            _ = window.UpdateStatistics();
        }

        public void AnyChangeHappen()
        {
            ProgramStateManager.IsAnyChange = true;
            ProgramStateManager.IsUnsavedChange = true;
            _ = window.UpdateStatistics();
        }

        public bool CloseFileAndAskToSave()
        {
            Debug.WriteLine("Stop with shutdown");
            if (ProgramStateManager.IsUnsavedChange)
            {
                Debug.WriteLine(_filmsFile.FilePath);
                if (_filmsFile.FilePath == "New File" || !SettingsManager.AutoSave)
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

        public void FilmRecord_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IdInList")
            {
                AnyChangeHappen();
            }
        }

        public void LoadLocally()
        {
            if (CloseFileAndAskToSave())
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
                    OpenFilepath(openFileDialog.FileName);
                }
            }
        }

        public void NewFile()
        {
            OpenFilepath(null);
        }

        public void OnSaveCompleted(RecordManager filmsFile)
        {
            SavedComplete?.Invoke(this, filmsFile);
        }

        public void OpenFilepath(string? newFilePath)
        {
            if (string.IsNullOrEmpty(newFilePath) || !(File.Exists(newFilePath)))
            {
                FilmsFile = new RecordManager();
                FilmsFile.StartReader();
            }
            else
            {
                FilmsFile = new RecordManager(newFilePath);
                FilmsFile.StartReader();
            }
            AfterFileHasBeenLoaded();
        }

        public void OpenFilepathButSaveChangesFirst(string? newFilePath)
        {
            if (string.IsNullOrEmpty(newFilePath) || !(File.Exists(newFilePath)))
            {
                if (CloseFileAndAskToSave())
                {
                    FilmsFile = new RecordManager();
                    FilmsFile.StartReader();
                }
                else
                {
                    return;
                }
            }
            else
            {
                FilmsFile = new RecordManager(newFilePath);
                FilmsFile.StartReader();
            }
            AfterFileHasBeenLoaded();
        }

        public bool Save()
        {
            string filePath = _filmsFile.FilePath;
            bool saved = false;

            if (string.IsNullOrEmpty(filePath))
            {
                saved = SaveAs();
                return saved;
            }

            _filmsFile.StartWriter(filePath);
            OnSaveCompleted(_filmsFile);
            ProgramStateManager.IsUnsavedChange = (false);

            // Return true if saving was successful
            return true;
        }

        public bool SaveAs()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save file as";
            saveFileDialog.Filter = "Text files (*.txt), (*.csv)|*.txt;*.csv";

            if (!string.IsNullOrEmpty(_filmsFile.FilePath))
            {
                string parentDirectory = Directory.GetParent(_filmsFile.FilePath)?.FullName;
                if (!string.IsNullOrEmpty(parentDirectory))
                {
                    saveFileDialog.InitialDirectory = parentDirectory;
                }
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                _filmsFile.StartWriter(saveFileDialog.FileName);
                OpenFilepath(saveFileDialog.FileName);
                return true;
            }
            return false;
        }

        public void SaveFileAtLocation(string filepath)
        {
            _filmsFile.StartWriter(filepath);
            OpenFilepath(filepath);
        }

        public void ScrollToBottomOfList()
        {
            if (FilmsObservableList.Count > 0)
                visualFilmsTable.ScrollIntoView(FilmsObservableList.ElementAt(FilmsObservableList.Count - 1));
        }

        public void setUpFilmsDataGrid(DataGrid dataGrid)
        {
            visualFilmsTable = dataGrid;
        }

        public void setUpMainWindow(MainWindow window)
        {
            this.window = window;
        }

        public void setUpStatisticsManager(StatisticsManager newStatisticsManager)
        {
            statisticsManager = newStatisticsManager;
        }

        public bool ShowSaveChangesDialog()
        {
            SaveChangesDialog dialog = new SaveChangesDialog();
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();

            switch (dialog.Result)
            {
                case SaveChangesDialog.CustomDialogResult.Save:
                    return false;
                //   return Save();

                case SaveChangesDialog.CustomDialogResult.NotSave:
                    return true;

                case SaveChangesDialog.CustomDialogResult.Cancel:
                default:
                    return false;
            }
        }

        private static void TryToOpenFilepath(string? filepath)
        {
            // just like openfilepath from mainwindow, open filepath but first check if unsaved changes
        }

        private void filmsListHasChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.Print("list changed listener");
            ProgramStateManager.AtLeastOneRecord = FilmsObservableList.Count > 0;
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
    }
}