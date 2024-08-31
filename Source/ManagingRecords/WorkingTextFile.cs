﻿using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.GUIimprovements;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.RecordValueValidator;
using WatchedFilmsTracker.Source.Views;
using static WatchedFilmsTracker.Source.Models.CommonCollections;

namespace WatchedFilmsTracker.Source.ManagingFilmsFile
{
    public class WorkingTextFile
    {
        public CollectionOfRecords CollectionOfRecords { get => _collectionOfFilms; set => _collectionOfFilms = value; }
        public CollectionType collectionType { get; set; }
        public DataGrid DataGrid { get => _dataGrid; set => _dataGrid = value; }
        public Action<object, RoutedEventArgs> DeleteRecordAction { get; set; }
        public ObservableCollection<RecordModel> FilmsObservableList { get => _filmsObservableList; set => _filmsObservableList = value; }
        public StatisticsManager StatisticsManager { get => _statisticsManager; set => _statisticsManager = value; }
        private CollectionOfRecords _collectionOfFilms;
        private DataGrid _dataGrid;
        private FilmRecordPropertyValidator _filmRecordPropertyValidator;
        private ObservableCollection<RecordModel> _filmsObservableList;
        private StatisticsManager _statisticsManager;
        private MainWindow _window;

        public WorkingTextFile()
        {
            FilmsObservableList = new ObservableCollection<RecordModel>();
            SystemAccentColour.Initialize();
            SystemAccentColour.AccentColorChanged += SystemAccentColour_AccentColorChanged;
        }

        public event EventHandler AnyChangeHappenedEvent;

        public event EventHandler<CollectionOfRecords> SavedComplete;

        //public void AddContextMenuForTheItem(DataGrid dataGrid)
        //{
        //    dataGrid.LoadingRow += (s, e) =>
        //    {
        //        FilmRecord? filmRecord = e.Row.DataContext as FilmRecord;
        //        if (filmRecord == null) return;

        //        // Create context menu
        //        ContextMenu contextMenu = new ContextMenu();

        //        // Delete record menu item
        //        MenuItem deleteRecordMenuItem = new MenuItem()
        //        {
        //            Header = "Delete record",
        //            Icon = new Image()
        //            {
        //                Source = new BitmapImage(new Uri("pack://application:,,,/Assets/ButtonIcons/deleteRecord.png"))
        //            }
        //        };
        //        deleteRecordMenuItem.Click += (sender, args) => this.CollectionOfFilms.DeleteRecordFromList(filmRecord);
        //        contextMenu.Items.Add(deleteRecordMenuItem);

        //        // Search film on the internet menu item
        //        MenuItem searchFilmOnTheInternetMenuItem = new MenuItem()
        //        {
        //            Header = "Search film on the internet",
        //            Icon = new Image()
        //            {
        //                Source = new BitmapImage(new Uri("pack://application:,,,/Assets/ButtonIcons/searchInternetForFilm.png"))
        //            }
        //        };
        //        searchFilmOnTheInternetMenuItem.Click += (sender, args) =>
        //        {
        //            string query = Uri.EscapeUriString($"{filmRecord.EnglishTitle} {filmRecord.ReleaseYear}");
        //            string url = $"https://www.google.com/search?q={query}";
        //            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        //        };
        //        contextMenu.Items.Add(searchFilmOnTheInternetMenuItem);

        //        e.Row.ContextMenu = contextMenu;
        //    };
        //}

        public void AfterFileHasBeenLoaded()
        {
            FilmsObservableList = CollectionOfRecords.ObservableCollectionOfRecords;
            DataGrid.ItemsSource = FilmsObservableList;

            // Subscribe to PropertyChanged event of each FilmRecord instance
            FilmsObservableList.CollectionChanged += filmsListHasChanged;

            foreach (var filmRecord in FilmsObservableList)
            {
                filmRecord.CellChanged += FilmRecord_PropertyChanged;
            }

            // Increase the brightness of the brightAccentColour by 80%
            _dataGrid.AlternatingRowBackground = new SolidColorBrush(SystemAccentColour.GetBrightAccentColourRGB());

            _dataGrid.CellEditEnding += CellEditEnding;

            SettingsManager.LastPath = CollectionOfRecords.FilePath;
            ProgramStateManager.IsUnsavedChange = false;
            ProgramStateManager.IsAnyChange = false;
            ProgramStateManager.AtLeastOneRecord = FilmsObservableList.Count > 0;

            if (string.IsNullOrEmpty(CollectionOfRecords.FilePath))
            {
                ProgramStateManager.IsFileSavedOnDisk = false;
                ProgramStateManager.IsFileInLocalMyDataFolder = false;
            }
            else
            {
                ProgramStateManager.IsFileSavedOnDisk = true;

                string lastDirectory = Directory.GetParent(CollectionOfRecords.FilePath).Name;
                Debug.WriteLine($"{lastDirectory} is folder where the file is in");
                if (lastDirectory == "MyData")
                    ProgramStateManager.IsFileInLocalMyDataFolder = true;
                else
                    ProgramStateManager.IsFileInLocalMyDataFolder = false;
            }

            CollectionOfRecords.CreateColumnsWithIds();

            StatisticsManager = new StatisticsManager(FilmsObservableList);
            _window.UpdateStageTitle();
            CollectionOfRecords.CloseReader();

            if (SettingsManager.ScrollLastPosition)
                ScrollToBottomOfList();

            //      AddContextMenuForTheItem(VisualFilmsTable);

            _ = _window.UpdateStatistics();
        }

        public void AnyChangeHappen()
        {
            ProgramStateManager.IsAnyChange = true;
            ProgramStateManager.IsUnsavedChange = true;
            _ = _window.UpdateStatistics();
            AnyChangeHappenedEvent?.Invoke(this, EventArgs.Empty);
        }

        public bool CloseFileAndAskToSave()
        {
            Debug.WriteLine("Stop with shutdown");
            if (ProgramStateManager.IsUnsavedChange)
            {
                Debug.WriteLine(_collectionOfFilms.FilePath);
                if (_collectionOfFilms.FilePath == "New File" || !SettingsManager.AutoSave)
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

        public void FilmRecord_PropertyChanged(object sender, CellChangedEventArgs e)
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

        public void NewFile(CollectionType collectionType)
        {

        }

        public void OnSaveCompleted(CollectionOfRecords filmsFile)
        {
            SavedComplete?.Invoke(this, filmsFile);
        }

        public void OpenFilepath(string? newFilePath)
        {
            if (string.IsNullOrEmpty(newFilePath) || !File.Exists(newFilePath))
            {
                CollectionOfRecords = new CollectionOfRecords(this);
                CollectionOfRecords.StartReader();
            }
            else
            {
                CollectionOfRecords = new CollectionOfRecords(newFilePath, this);
                CollectionOfRecords.StartReader();
            }
            AfterFileHasBeenLoaded();
        }

        public void OpenFilepathButSaveChangesFirst(string? newFilePath)
        {
            if (string.IsNullOrEmpty(newFilePath) || !File.Exists(newFilePath))
            {
                if (CloseFileAndAskToSave())
                {
                    CollectionOfRecords = new CollectionOfRecords(this);
                    CollectionOfRecords.StartReader();
                }
                else
                {
                    return;
                }
            }
            else
            {
                CollectionOfRecords = new CollectionOfRecords(newFilePath, this);
                CollectionOfRecords.StartReader();
            }
            AfterFileHasBeenLoaded();
        }

        public bool Save()
        {
            string filePath = _collectionOfFilms.FilePath;
            bool saved = false;

            if (string.IsNullOrEmpty(filePath))
            {
                saved = SaveAs();
                return saved;
            }

            _collectionOfFilms.StartWriter(filePath);
            OnSaveCompleted(_collectionOfFilms);
            ProgramStateManager.IsUnsavedChange = false;

            // Return true if saving was successful
            return true;
        }

        public bool SaveAs()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save file as";
            saveFileDialog.Filter = "Text files (*.txt), (*.csv)|*.txt;*.csv";

            if (!string.IsNullOrEmpty(_collectionOfFilms.FilePath))
            {
                string parentDirectory = Directory.GetParent(_collectionOfFilms.FilePath)?.FullName;
                if (!string.IsNullOrEmpty(parentDirectory))
                {
                    saveFileDialog.InitialDirectory = parentDirectory;
                }
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                _collectionOfFilms.StartWriter(saveFileDialog.FileName);
                OpenFilepath(saveFileDialog.FileName);
                return true;
            }
            return false;
        }

        public void SaveFileAtLocation(string filepath)
        {
            _collectionOfFilms.StartWriter(filepath);
            OpenFilepath(filepath);
        }

        public void ScrollToBottomOfList()
        {
            if (FilmsObservableList.Count > 0)
                DataGrid.ScrollIntoView(FilmsObservableList.ElementAt(FilmsObservableList.Count - 1));
        }

        public void setUpFilmsDataGrid(DataGrid dataGrid)
        {
            DataGrid = dataGrid;
        }

        public void setUpMainWindow(MainWindow window)
        {
            _window = window;
        }

        public void setUpStatisticsManager(StatisticsManager newStatisticsManager)
        {
            StatisticsManager = newStatisticsManager;
        }

        public bool ShowSaveChangesDialog()
        {
            SaveChangesDialog dialog = new SaveChangesDialog();
            dialog.Owner = Application.Current.MainWindow;
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

        private void CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit && "Release year" == e.Column.Header.ToString())
            {
                // Get the new value
                var editedCell = e.EditingElement as TextBox;
                if (editedCell != null)
                {
                    string newValue = editedCell.Text;
                }

                // Change the cell background color to red
                DataGridRow row = e.Row;
                DataGridCell cell = DataGridCellGetter.GetCell(_dataGrid, e.Row.GetIndex(), e.Column.DisplayIndex);

                // Pass the FilmRecord value to the IsReleaseYearValid method
                if (e.Row.DataContext is RecordModel filmRecord)
                {
                    //  Debug.WriteLine("committed value is: " + _filmRecordPropertyValidator.IsReleaseYearValid(editedCell.Text));
                    //   DataGridCellAppearanceHelper.MakeCellAppearInvalid(cell, !_filmRecordPropertyValidator.IsReleaseYearValid(editedCell.Text));
                    //todo fix validator
                }
            }
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
                    if (newItem is RecordModel newRecord)
                    {
                        // Subscribe to PropertyChanged event of the new FilmRecord instance
                        //   newRecord.PropertyChanged += FilmRecord_PropertyChanged;
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                // Items removed from the list
                foreach (var oldItem in e.OldItems)
                {
                    if (oldItem is RecordModel oldRecord)
                    {
                        // Unsubscribe from PropertyChanged event of the removed FilmRecord instance
                        // oldRecord.PropertyChanged -= FilmRecord_PropertyChanged;
                    }
                }
            }
        }

        private void SystemAccentColour_AccentColorChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("Accent colour changed");
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Ensure UI updates happen on the UI thread

                _dataGrid.AlternatingRowBackground = new SolidColorBrush(SystemAccentColour.GetBrightAccentColourRGB());
            });
        }
    }
}