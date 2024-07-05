using System.Collections.ObjectModel;
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
        private string fileColumns;
        private string filePath;
        private FilmsTextFile filmsFileHandler;
        private ObservableCollection<FilmRecord> listOfFilms;
        private CSVreader reader;
        private CSVwriter writer;

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

        public string FilePath
        {
            get { return filePath; }
            set { filePath = value; }
        }

        public ObservableCollection<FilmRecord> ListOfFilms
        {
            get { return listOfFilms; }
        }

        public void DeleteAllRecords()
        {
            filmsFileHandler.CollectionOfFilms.ListOfFilms.Clear();
            filmsFileHandler.AnyChangeHappen();
        }

        public void AddEmptyRecordToList()
        {
            FilmRecord newRecord = new FilmRecord(listOfFilms.Count + 1);
            newRecord.PropertyChanged += filmsFileHandler.FilmRecord_PropertyChanged;
            listOfFilms.Add(newRecord);
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
            filmsFileHandler.AnyChangeHappen();
        }

        public void CloseReader()
        {
            reader.CloseFile();
        }

        public void DeleteRecordFromList(FilmRecord selected)
        {
            if (selected != null)
            {
                int selectedIndex = filmsFileHandler.VisualFilmsTable.SelectedIndex;
                if (selectedIndex == filmsFileHandler.VisualFilmsTable.Items.Count)
                    filmsFileHandler.VisualFilmsTable.SelectedIndex = selectedIndex - 1;
                else
                {
                    filmsFileHandler.VisualFilmsTable.SelectedIndex = selectedIndex;
                }
                filmsFileHandler.AnyChangeHappen();
            }

            if (listOfFilms.Count == 0 || selected.IdInList == 0) return;
            int idOfSelected = selected.IdInList;
            listOfFilms.Remove(selected);
            RefreshFurtherIDs(idOfSelected);
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

        public void StartReader()
        {
            reader = new CSVreader(filePath);
            fileColumns = reader.GetFileColumns();
            LoadRecordsFromCSVToArray(reader);
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
    }
}