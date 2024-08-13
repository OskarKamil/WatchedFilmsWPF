using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.Services.Csv;

namespace WatchedFilmsTracker.Source.ManagingFilmsFile
{
    /// <summary>
    /// Represents a collection of films. This class is responsible for storing a list of films
    /// and managing all operations on this list, such as adding, removing, or updating film records.
    /// All interactions with the film list should go through this class.
    /// </summary>
    public class CollectionOfRecords
    {
        public List<int> ColumnRepresentation
        {
            get => _columnRepresentation;
            set
            {
                _columnRepresentation = value;
            }
        }

        public string FilePath
        {
            get { return filePath; }
            set { filePath = value; }
        }

        public ObservableCollection<RecordModel> ListOfFilms
        {
            get { return listOfFilms; }
        }

        private List<int> _columnRepresentation;
        private List<Column> columns;

        private DataGridManager dataGridInitialiser;

        private string fileColumnHeaders;

        private string filePath;
        private WorkingTextFile filmsFileHandler;

        private ObservableCollection<RecordModel> listOfFilms;

        private CSVreader reader;

        private CSVwriter writer;

        public CollectionOfRecords(string filePath, WorkingTextFile filmsTextFile)
        {
            this.filmsFileHandler = filmsTextFile;
            this.filePath = filePath;
            _columnRepresentation = new List<int>();
        }

        public CollectionOfRecords(WorkingTextFile filmsTextFile)
        {
            this.filmsFileHandler = filmsTextFile;
            filePath = null;
            _columnRepresentation = new List<int>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //public void AddEmptyRecordToList()
        //{
        //    FilmRecord newRecord = new FilmRecord(listOfFilms.Count + 1);
        //    newRecord.PropertyChanged += filmsFileHandler.FilmRecord_PropertyChanged;
        //    listOfFilms.Add(newRecord);
        //    filmsFileHandler.VisualFilmsTable.SelectedCells.Clear();
        //    if (filmsFileHandler.VisualFilmsTable.ItemsSource == listOfFilms)
        //    {
        //        filmsFileHandler.VisualFilmsTable.SelectedItem = newRecord;
        //        filmsFileHandler.VisualFilmsTable.ScrollIntoView(filmsFileHandler.VisualFilmsTable.SelectedItem);
        //    }

        //    if (SettingsManager.DefaultDateIsToday)
        //    {
        //        string formattedString = DateTime.Now.ToString("dd/MM/yyyy");
        //        newRecord.WatchDate = (formattedString);
        //    }

        //    StartEditingNewRecord();

        //    filmsFileHandler.AnyChangeHappen();
        //}

        public void CloseReader()
        {
            reader.CloseFile();
        }

        public void DeleteAllRecords()
        {
            filmsFileHandler.CollectionOfFilms.ListOfFilms.Clear();
            filmsFileHandler.AnyChangeHappen();
        }

        //public void DeleteRecordFromList(FilmRecord selected)
        //{
        //    if (selected == null) { return; }

        //    int selectedIndex = filmsFileHandler.VisualFilmsTable.Items.IndexOf(selected);

        //    if (listOfFilms.Count == 0 || selected.IdInList == 0) return;
        //    int idOfSelected = selected.IdInList;
        //    listOfFilms.Remove(selected);
        //    RefreshFurtherIDs(idOfSelected);

        //    // selecting the next record
        //    filmsFileHandler.VisualFilmsTable.SelectedCells.Clear();
        //    if (selectedIndex + 0 == filmsFileHandler.VisualFilmsTable.Items.Count)
        //    {
        //        filmsFileHandler.VisualFilmsTable.SelectedIndex = selectedIndex - 1;
        //    }
        //    else
        //    {
        //        filmsFileHandler.VisualFilmsTable.SelectedIndex = selectedIndex - 0;
        //    }
        //}

        //public void RefreshFurtherIDs(int idOfSelected)
        //{
        //    for (int i = 0; i < listOfFilms.Count; i++)
        //    {
        //        FilmRecord record = listOfFilms[i];
        //        if (record.IdInList >= idOfSelected) record.IdInList--;
        //    }
        //}

        public void StartReader()
        {
            reader = new CSVreader();
            listOfFilms = reader.ReadCsvReturnObservableCollection(filePath);
            columns = reader.GetColumns();
            DataGridManager.BuildColumnsFromList(columns, filmsFileHandler.DataGrid);
        }

        public void StartWriter(string newFilePath)
        {
            if (reader != null) CloseReader();
            writer = new CSVwriter(newFilePath);
            List<int> visibleColumns = GetIdsOfVisibleProperties();

            
            writer.SaveListIntoCSV(listOfFilms.ToList(), filmsFileHandler.DataGrid, visibleColumns);
            CloseWriter();
        }

        private void CloseWriter()
        {
            writer?.Close();
        }

        private List<int> GetIdsOfVisibleProperties()
        {
            List<int> ids = new List<int>();
            for (int i = 0; i < _columnRepresentation.Count; i++)
            {
                if (_columnRepresentation[i] != -1)
                    ids.Add(_columnRepresentation[i]);
            }
            return ids;
        }

        private void StartEditingNewRecord()
        {
            filmsFileHandler.DataGrid.CurrentCell = new DataGridCellInfo(filmsFileHandler.DataGrid.Items[filmsFileHandler.DataGrid.Items.Count - 1], filmsFileHandler.DataGrid.Columns[1]);

            filmsFileHandler.DataGrid.BeginEdit();
        }
    }
}