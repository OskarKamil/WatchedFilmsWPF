using System.Collections.ObjectModel;
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
        public string FilePath
        {
            get { return filePath; }
            set { filePath = value; }
        }

        public ObservableCollection<RecordModel> ListOfFilms
        {
            get { return listOfFilms; }
        }

        private DataGridInitialiser dataGridInitialiser;
        private string fileColumns;
        private string filePath;
        private WorkingTextFile filmsFileHandler;
        private ObservableCollection<RecordModel> listOfFilms;
        private CSVreader reader;
        private CSVwriter writer;
        private List<Column> columns;

        public CollectionOfRecords(string filePath, WorkingTextFile filmsTextFile)
        {
            this.filmsFileHandler = filmsTextFile;
            this.filePath = filePath;
        }

        public CollectionOfRecords(WorkingTextFile filmsTextFile)
        {
            this.filmsFileHandler = filmsTextFile;
            filePath = null;
        }

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

        public void StartNewReader()
        {
            reader = new CSVreader();
            listOfFilms = reader.ReadCsvReturnObservableCollection(filePath);
            columns = reader.GetColumns();
            DataGridInitialiser.BuildColumnsFromList(columns, filmsFileHandler.DataGrid);
        }



        public void StartWriter(string newFilePath)
        {
            if (reader != null) CloseReader();
            writer = new CSVwriter(newFilePath);
            writer.SetFileColumn(fileColumns);
            writer.SaveListIntoCSV(listOfFilms.ToList());
            CloseWriter();
        }

        private void CloseWriter()
        {
            writer?.Close();
        }

        private void StartEditingNewRecord()
        {
            filmsFileHandler.DataGrid.CurrentCell = new DataGridCellInfo(filmsFileHandler.DataGrid.Items[filmsFileHandler.DataGrid.Items.Count - 1], filmsFileHandler.DataGrid.Columns[1]);

            filmsFileHandler.DataGrid.BeginEdit();
        }
    }
}