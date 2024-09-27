using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WatchedFilmsTracker.Source.Buttons;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.GUIimprovements;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.Models;
using WatchedFilmsTracker.Source.RecordValueValidator;
using WatchedFilmsTracker.Source.Statistics;
using WatchedFilmsTracker.Source.Views;
using static WatchedFilmsTracker.Source.Models.CommonCollections;

namespace WatchedFilmsTracker.Source.ManagingFilmsFile
{
    public class WorkingTextFile
    {
        public bool AnyChange { get; set; }

        public CollectionOfRecords CollectionOfRecords { get; set; }

        public CommonCollectionType CommonCollectionType
        {
            get => _commonCollectionType;
            set
            {
                if (_commonCollectionType != value)
                {
                    _commonCollectionType = value;
                    CommonCollectionTypeChanged?.Invoke(this, new CommonCollectionTypeChangedEventArgs(_commonCollectionType));
                }
            }
        }

        public DataGrid DataGrid { get; set; }

        public Action<object, RoutedEventArgs> DeleteRecordAction { get; set; }

        public Grid Grid { get; set; }

        public StatisticsManager StatisticsManager { get; set; }

        public bool UnsavedChanges
        {
            get => _unsavedChanges;
            set
            {
                if (_unsavedChanges != value)
                {
                    _unsavedChanges = value;
                    ButtonStateManager.UpdateUnsavedChanges(this);
                }
            }
        }

        public string FilePath;

        private CommonCollectionType _commonCollectionType;

        private FilmRecordPropertyValidator _filmRecordPropertyValidator;

        private StatisticsManager _statisticsManager;

        private bool _unsavedChanges;

        public WorkingTextFile(CommonCollectionType commonCollection)
        {
            CommonCollectionType = commonCollection;

            DataGrid = CreateGridInTheUI();

            CollectionOfRecords = new CollectionOfRecords("", this);
            DataGrid.ItemsSource = GetObservableCollectionOfRecords();

            CollectionOfRecords.DataGridManager = new DataGridManager(DataGrid);
            CollectionOfRecords.CreateColumnsInBlankFile();
            AfterFileHasBeenLoaded();
        }

        public WorkingTextFile(string filePath)
        {
            CommonCollectionType = CommonCollections.GetCommonCollectionByName(CollectionType.Other);

            DataGrid = CreateGridInTheUI();
            FilePath = filePath;
            CollectionOfRecords = new CollectionOfRecords(filePath, this);

            DataGrid.ItemsSource = GetObservableCollectionOfRecords();

            CollectionOfRecords.DataGridManager = new DataGridManager(DataGrid);
            AfterFileHasBeenLoaded();
        }

        public event Action CollectionHasChanged;

        public event EventHandler<CommonCollectionTypeChangedEventArgs> CommonCollectionTypeChanged;

        public event EventHandler FileClosing;

        public event EventHandler<CollectionOfRecords> SavedComplete;

        public void AfterFileHasBeenLoaded()
        {
            UnsavedChanges = false;
            GetObservableCollectionOfRecords().CollectionChanged += filmsListHasChanged;

            foreach (var filmRecord in GetObservableCollectionOfRecords())
            {
                filmRecord.CellChanged += FilmRecord_PropertyChanged;
            }

            DataGrid.AlternatingRowBackground = new SolidColorBrush(SystemAccentColour.GetBrightAccentColourRGB());

            DataGrid.CellEditEnding += CellEditEnding;

            SettingsManager.LastPath = FilePath;

            CollectionOfRecords.CreateColumnsWithIds();

            StatisticsManager = new StatisticsManager(GetObservableCollectionOfRecords());

            CollectionOfRecords.CloseReader();

            if (SettingsManager.ScrollLastPosition)
                ScrollToBottomOfList();

            //      AddContextMenuForTheItem(VisualFilmsTable);
            //        e.Row.ContextMenu = contextMenu;
            //    };
            //}

            TabsWorkingTextFiles.MainWindow.UpdateStatistics();

            DataGrid.SelectedCellsChanged += (obs, args) =>
            {
                Debug.WriteLine("Selected cells changes, or deselected if filtered");
                ButtonStateManager.UpdateSelectedCells(this);
            };
        }

        public void AnyChangeHappen()
        {
            AnyChange = true;
            UnsavedChanges = true;
            CollectionHasChanged?.Invoke();
            TabsWorkingTextFiles.MainWindow.UpdateStatistics();
        }

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

        public DataGrid CreateGridInTheUI()
        {
            Grid grid = new Grid
            {
                Name = "grid"
            };
            Grid = grid;

            DataGrid dataGrid = new DataGrid
            {
                Name = "dataGrid",
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                ColumnWidth = DataGridLength.Auto,
                CanUserDeleteRows = false,
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(0xD6, 0xE2, 0xFF)),
                AlternationCount = 4,
                CanUserResizeRows = false,
                SelectionUnit = DataGridSelectionUnit.CellOrRowHeader,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Visible
            };

            // Set ScrollViewer properties on the DataGrid
            ScrollViewer.SetCanContentScroll(dataGrid, true);
            ScrollViewer.SetVerticalScrollBarVisibility(dataGrid, ScrollBarVisibility.Auto);
            ScrollViewer.SetHorizontalScrollBarVisibility(dataGrid, ScrollBarVisibility.Auto);

            // Add the DataGrid to the Grid
            grid.Children.Add(dataGrid);

            return dataGrid;
        }

        public bool DoesExistInLocalMyDataFolder()
        {
            if (string.IsNullOrEmpty(FilePath))

            {
                return false;
            }
            else
            {
                string filePath = Directory.GetParent(FilePath).Name;
                Debug.WriteLine($"{filePath} is folder where the file is in");
                if (filePath == "MyData")
                    return true;
                else
                    return true;
            }
        }

        public bool DoesExistOnDisk()
        {
            if (string.IsNullOrEmpty(FilePath))

            {
                return false;
            }
            else
            {
                return true;
            }
        }

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
        public void FilmRecord_PropertyChanged(object sender, CellChangedEventArgs e)
        {
            if (e.PropertyName != "IdInList")
            {
                AnyChangeHappen();
            }
        }

        public ObservableCollection<RecordModel> GetObservableCollectionOfRecords()
        {
            return CollectionOfRecords.ObservableCollectionOfRecords;
        }

        public IList<DataGridCellInfo> GetSelectedCells()
        {
            return DataGrid.SelectedCells;
        }

        public bool HasAtLeastOneRecord()
        {
            return GetObservableCollectionOfRecords().Count > 0;
        }

        public bool HasSelectedCells()
        {
            return DataGrid.SelectedCells.Count > 0;
        }

        public bool HasUnsavedChanges()
        {
            return UnsavedChanges;
        }

        public void NewFile()
        {
            OpenFilepath(null);
        }

        //        // Create context menu
        //        ContextMenu contextMenu = new ContextMenu();
        public void NewFile(CollectionType collectionType)
        {
        }

        //public void AddContextMenuForTheItem(DataGrid dataGrid)
        //{
        //    dataGrid.LoadingRow += (s, e) =>
        //    {
        //        FilmRecord? filmRecord = e.Row.DataContext as FilmRecord;
        //        if (filmRecord == null) return;
        public void OnSaveCompleted(CollectionOfRecords filmsFile)
        {
            SavedComplete?.Invoke(this, filmsFile);
        }

        public void OpenFilepath(string? newFilePath)
        {
            if (string.IsNullOrEmpty(newFilePath) || !File.Exists(newFilePath))
            {
                CollectionOfRecords = new CollectionOfRecords("", this);
                CollectionOfRecords.ReadTextFile(FilePath);
            }
            else
            {
                CollectionOfRecords = new CollectionOfRecords(newFilePath, this);
                CollectionOfRecords.ReadTextFile(FilePath);
            }
            AfterFileHasBeenLoaded();
        }

        public void OpenFilepathButSaveChangesFirst(string? newFilePath)
        {
            if (string.IsNullOrEmpty(newFilePath) || !File.Exists(newFilePath))
            {
                if (TryToCloseFile())
                {
                    CollectionOfRecords = new CollectionOfRecords("", this);
                    CollectionOfRecords.ReadTextFile(FilePath);
                }
                else
                {
                    return;
                }
            }
            else
            {
                CollectionOfRecords = new CollectionOfRecords(newFilePath, this);
                CollectionOfRecords.ReadTextFile(FilePath);
            }
            AfterFileHasBeenLoaded();
        }

        /// <returns>
        /// Returns true is file has been saved. Returns false is file has not been saved.
        /// </returns>
        public bool Save()
        {
            string filePath = FilePath;
            bool saved = false;

            if (string.IsNullOrEmpty(filePath) || !Path.Exists(Path.GetDirectoryName(filePath)))
            {
                saved = SaveAs();
                return saved;
            }

            CollectionOfRecords.StartWriter(filePath);
            OnSaveCompleted(CollectionOfRecords);
            UnsavedChanges = false;

            return true;
        }

        /// <returns>
        /// Returns true if user pressed Save. Returns false if user pressed Cancel or closed the window.
        /// </returns>
        public bool SaveAs()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save file as";
            saveFileDialog.Filter = "Text files (*.txt), (*.csv)|*.txt;*.csv";

            if (!string.IsNullOrEmpty(FilePath))
            {
                string parentDirectory = Directory.GetParent(FilePath)?.FullName;
                if (!string.IsNullOrEmpty(parentDirectory))
                {
                    saveFileDialog.InitialDirectory = parentDirectory;
                }
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                CollectionOfRecords.StartWriter(saveFileDialog.FileName);
                OpenFilepath(saveFileDialog.FileName);
                UnsavedChanges = false;
                return true;
            }
            return false;
        }

        public void SaveFileAtLocation(string filepath)
        {
            CollectionOfRecords.StartWriter(filepath);
            OpenFilepath(filepath);
        }

        public void ScrollToBottomOfList()
        {
            if (GetObservableCollectionOfRecords().Count > 0)
                DataGrid.ScrollIntoView(GetObservableCollectionOfRecords().ElementAt(GetObservableCollectionOfRecords().Count - 1));
        }

        public void setUpFilmsDataGrid(DataGrid dataGrid)
        {
            DataGrid = dataGrid;
        }

        public void setUpStatisticsManager(StatisticsManager newStatisticsManager)
        {
            StatisticsManager = newStatisticsManager;
        }

        public SaveChangesDialog.CustomDialogResult ShowSaveChangesDialog()
        {
            SaveChangesDialog dialog = new SaveChangesDialog();
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();

            return dialog.Result;
        }

        ///  <summary>
        /// If file has unsaved changes, dialog asking to save changes will display.
        /// </summary>
        /// <returns>
        /// Returns whether file can be closed.
        /// </returns>
        public bool TryToCloseFile()
        {
            if (UnsavedChanges)
            {
                // New file not on disk or autosave off
                // Ask to save because file not on disk, could be a draft so ask
                // Autosave off so ask
                if (string.IsNullOrEmpty(FilePath) || SettingsManager.AutoSave == false)
                {
                    SaveChangesDialog.CustomDialogResult result = ShowSaveChangesDialog();
                    if (result == SaveChangesDialog.CustomDialogResult.Save)
                    {
                        bool canClose = Save();
                        if (canClose)
                        {
                            RaiseOnClosingFile();
                        }
                        return canClose;
                    }
                    else if (result == SaveChangesDialog.CustomDialogResult.NotSave)
                    {
                        RaiseOnClosingFile();
                        return true;
                    }
                    else if (result == SaveChangesDialog.CustomDialogResult.Cancel)
                    {
                        return false;
                    }
                    return false;
                }

                // File existing on disk
                else if (SettingsManager.AutoSave)
                {
                    bool canClose = Save();
                    if (canClose)
                    {
                        RaiseOnClosingFile();
                    }
                    return canClose;
                }
            }
            RaiseOnClosingFile();
            return UnsavedChanges;
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
                DataGridCell cell = DataGridCellGetter.GetCell(DataGrid, e.Row.GetIndex(), e.Column.DisplayIndex);

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
            ButtonStateManager.UpdateAtLeastOneRecord(this);
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

        private void RaiseOnClosingFile()
        {
            Debug.WriteLine("closing file event raised");
            FileClosing?.Invoke(this, EventArgs.Empty);
        }

        private void SystemAccentColour_AccentColorChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("Accent colour changed");
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Ensure UI updates happen on the UI thread

                DataGrid.AlternatingRowBackground = new SolidColorBrush(SystemAccentColour.GetBrightAccentColourRGB());
            });
        }

        public class CommonCollectionTypeChangedEventArgs : EventArgs
        {
            public CommonCollectionType NewCommonCollectionType { get; }

            public CommonCollectionTypeChangedEventArgs(CommonCollectionType newCommonCollectionType)
            {
                NewCommonCollectionType = newCommonCollectionType;
            }
        }
    }
}