using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.Managers;
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

        private DataGridManager dataGridManager;

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

        public void AddEmptyRecordToList()
        {
            // RecordModel newRecord = new RecordModel(listOfFilms.Count + 1);
            //todo implement id column
            RecordModel newRecord = new RecordModel(new List<Cell>());
            for (int i = 0; i < _columnRepresentation.Count; i++)
            {
                {
                    newRecord.Cells.Add(new Cell(string.Empty));
                }
            }

            newRecord.CellChanged += filmsFileHandler.FilmRecord_PropertyChanged;
            listOfFilms.Add(newRecord);
            filmsFileHandler.DataGrid.SelectedCells.Clear();
            if (filmsFileHandler.DataGrid.ItemsSource == listOfFilms)
            {
                filmsFileHandler.DataGrid.SelectedItem = newRecord;
                filmsFileHandler.DataGrid.ScrollIntoView(filmsFileHandler.DataGrid.SelectedItem);
            }

            if (SettingsManager.DefaultDateIsToday)
            {
                string formattedString = DateTime.Now.ToString("dd/MM/yyyy");
                int columnID = dataGridManager.GetIdOfColumnByHeader("Watch date");
                if (columnID != -1)
                    newRecord.Cells[_columnRepresentation[columnID]].Value = formattedString;
            }

            StartEditingNewRecord();

            filmsFileHandler.AnyChangeHappen();
        }

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
        //FilmRecord record = listOfFilms[i];
        //        if (record.IdInList >= idOfSelected) record.IdInList--;
        //    }
        //}

        public void StartReader()
        {
            reader = new CSVreader();
            listOfFilms = reader.ReadCsvReturnObservableCollection(filePath);
            columns = reader.GetColumns();
            for (int i = 0; i < columns.Count; i++)
            {
                ColumnRepresentation.Add((i));
            }
            dataGridManager = new DataGridManager(filmsFileHandler.DataGrid);
            dataGridManager.BuildColumnsFromList(columns);
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
            if (dataGridManager.GetNumberOfColumns() == 0)
                return;

            int columnID = dataGridManager.GetIdOfColumnByHeader("English title");
            if (columnID != -1)
                filmsFileHandler.DataGrid.CurrentCell = new DataGridCellInfo(filmsFileHandler.DataGrid.Items[filmsFileHandler.DataGrid.Items.Count - 1], filmsFileHandler.DataGrid.Columns[_columnRepresentation[columnID]]);

            filmsFileHandler.DataGrid.BeginEdit();
        }
    }
}