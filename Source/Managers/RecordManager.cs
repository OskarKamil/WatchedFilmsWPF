using System.Collections.ObjectModel;
using System.Globalization;
using WatchedFilmsTracker.Source.Models;
using WatchedFilmsTracker.Source.Services.Csv;

namespace WatchedFilmsTracker.Source.Managers
{
    public class RecordManager
    {
        private ObservableCollection<FilmRecord> listOfFilms;
        private string fileColumns;
        private CSVreader reader;
        private CSVwriter writer;
        private string filePath;

        public RecordManager(string filePath)
        {
            this.filePath = filePath;
        }

        public RecordManager()
        {
            this.filePath = null;
        }

        public void CloseReader()
        {
            reader.CloseFile();
        }

        private void CloseWriter()
        {
            writer?.Close();
        }

        public void DeleteRecordFromList(FilmRecord selected)
        {
            if (listOfFilms.Count == 0 || selected.IdInList == 0) return;
            int idOfSelected = selected.IdInList;
            listOfFilms.Remove(selected);
            RefreshFurtherIDs(idOfSelected);
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
    }
}