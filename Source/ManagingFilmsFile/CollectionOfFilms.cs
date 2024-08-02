using System.Collections.ObjectModel;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.Models;
using WatchedFilmsTracker.Source.Services.Csv;

namespace WatchedFilmsTracker.Source.ManagingFilmsFile
{
    /// <summary>
    /// Represents a collection of films. This class is responsible for storing a list of films
    /// and managing all operations on this list, such as adding, removing, or updating film records.
    /// All interactions with the film list should go through this class.
    /// </summary>
    public class CollectionOfFilms
    {
        public string FilePath
        {
            get { return filePath; }
            set { filePath = value; }
        }

        public ObservableCollection<FilmRecord> ListOfFilms
        {
            get { return listOfFilms; }
        }

        private DataGridManager dataGridManager;
        private string fileColumns;
        private string filePath;
        private FilmsTextFile filmsFileHandler;
        private ObservableCollection<FilmRecord> listOfFilms;
        private CSVreader reader;
        private CSVwriter writer;
        // todo list of columns

        public CollectionOfFilms(string filePath, FilmsTextFile filmsTextFile)
        {
            this.filmsFileHandler = filmsTextFile;
            this.filePath = filePath;
        }

        public CollectionOfFilms(FilmsTextFile filmsTextFile)
        {
            this.filmsFileHandler = filmsTextFile;
            filePath = null;
        }

        public void AddEmptyRecordToList()
        {
            FilmRecord newRecord = new FilmRecord(listOfFilms.Count + 1);
            newRecord.PropertyChanged += filmsFileHandler.FilmRecord_PropertyChanged;
            listOfFilms.Add(newRecord);
            filmsFileHandler.VisualFilmsTable.SelectedCells.Clear();
            if (filmsFileHandler.VisualFilmsTable.ItemsSource == listOfFilms)
            {
                filmsFileHandler.VisualFilmsTable.SelectedItem = newRecord;
                filmsFileHandler.VisualFilmsTable.ScrollIntoView(filmsFileHandler.VisualFilmsTable.SelectedItem);
            }

            if (SettingsManager.DefaultDateIsToday)
            {
                string formattedString = DateTime.Now.ToString("dd/MM/yyyy");
                newRecord.WatchDate = (formattedString);
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

        public void DeleteRecordFromList(FilmRecord selected)
        {
            if (selected == null) { return; }

            int selectedIndex = filmsFileHandler.VisualFilmsTable.Items.IndexOf(selected);

            if (listOfFilms.Count == 0 || selected.IdInList == 0) return;
            int idOfSelected = selected.IdInList;
            listOfFilms.Remove(selected);
            RefreshFurtherIDs(idOfSelected);

            // selecting the next record
            filmsFileHandler.VisualFilmsTable.SelectedCells.Clear();
            if (selectedIndex + 0 == filmsFileHandler.VisualFilmsTable.Items.Count)
            {
                filmsFileHandler.VisualFilmsTable.SelectedIndex = selectedIndex - 1;
            }
            else
            {
                filmsFileHandler.VisualFilmsTable.SelectedIndex = selectedIndex - 0;
            }
        }

        public void LoadRecordsFromCSVToArray(CSVreader CSVfile)
        {
            listOfFilms = new ObservableCollection<FilmRecord>(CSVfile.GetAllFilmsRecordsFromFile());
        }

        public void RefreshFurtherIDs(int idOfSelected)
        {
            for (int i = 0; i < listOfFilms.Count; i++)
            {
                FilmRecord record = listOfFilms[i];
                if (record.IdInList >= idOfSelected) record.IdInList--;
            }
        }

        public void StartNewReader()
        {
            reader = new CSVreader();
            listOfFilms = reader.ReadCsvReturnObservableCollection(filePath);
        }

        public void StartReader()
        {
            reader = new CSVreader(filePath);
            fileColumns = reader.GetFileColumns();
            LoadRecordsFromCSVToArray(reader);

            // todo noralize filmrecord dictionaries
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
            filmsFileHandler.VisualFilmsTable.CurrentCell = new DataGridCellInfo(filmsFileHandler.VisualFilmsTable.Items[filmsFileHandler.VisualFilmsTable.Items.Count - 1], filmsFileHandler.VisualFilmsTable.Columns[1]);

            filmsFileHandler.VisualFilmsTable.BeginEdit();
        }
    }
}