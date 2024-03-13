using System.IO;
using System.Text;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Services.Csv
{
    public class CSVreader
    {
        private string filePath;
        private StreamReader filmsFile;
        private string fileColumns;
        private string lineFromFile;
        private List<string> valuesFromLine;
        private IEnumerator<string> iterator;

        public CSVreader(string newFilePath)
        {
            filePath = newFilePath;
            if (File.Exists(newFilePath))
            {
                try
                {
                    filmsFile = new StreamReader(filePath, System.Text.Encoding.UTF8);
                }
                catch (IOException)
                {
                    Console.Error.WriteLine("File not found.");
                }
            }
            else
            {
                string defaultNewFile = "Title\tOriginal Title\tType\tRelease year\tRating\tWatch date\tComments\t";
                filmsFile = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(defaultNewFile)));
                filePath = "";
                SettingsManager.LastPath = "";
            }

            ReadFirstLine();
            PrepareValuesFromCurrentLine();
            Console.Write("This CSV file's structures is: ");
            while (iterator.MoveNext()) Console.Write("[" + iterator.Current + "] ");
            Console.WriteLine();
        }

        public void CloseFile()
        {
            filmsFile.Close();
        }

        public List<FilmRecord> GetAllFilmsRecordsFromFile()
        {
            List<FilmRecord> listOfAllFilms = new List<FilmRecord>(3000);
            while (HasNextLine())
            {
                FilmRecord newRecord = GetNextFilmRecordFromFile();
                newRecord.IdInList = listOfAllFilms.Count + 1;
                listOfAllFilms.Add(newRecord);
            }
            return listOfAllFilms;
        }

        public string GetFileColumns()
        {
            return fileColumns;
        }

        public string GetFilePath()
        {
            return filePath;
        }

        private string ReadFileColumns()
        {
            fileColumns = NextLine();
            return fileColumns;
        }

        public FilmRecord GetNextFilmRecordFromFile()
        {
            ReadNextLine();
            PrepareValuesFromCurrentLine();
            FilmRecord record = new FilmRecord();
            if (iterator.MoveNext()) record.EnglishTitle = iterator.Current;
            if (iterator.MoveNext()) record.OriginalTitle = iterator.Current;
            if (iterator.MoveNext()) record.Type = iterator.Current;
            if (iterator.MoveNext()) record.ReleaseYear = iterator.Current;
            if (iterator.MoveNext()) record.Rating = iterator.Current;
            if (iterator.MoveNext()) record.WatchDate = iterator.Current;
            if (iterator.MoveNext()) record.Comments = iterator.Current;
            return record;
        }

        public bool HasNextLine()
        {
            return !filmsFile.EndOfStream;
        }

        public string NextLine()
        {
            if (HasNextLine()) return filmsFile.ReadLine();
            else return "";
        }

        public string NextValueFromLine()
        {
            if (valuesFromLine == null)
            {
                string[] valuesArray = lineFromFile.Split('\t');
                valuesFromLine = new List<string>(valuesArray);
                iterator = valuesFromLine.GetEnumerator();
            }
            iterator.MoveNext();
            return iterator.Current;
        }

        public void PrepareValuesFromCurrentLine()
        {
            string[] valuesArray = lineFromFile.Split('\t');
            valuesFromLine = new List<string>(valuesArray);
            iterator = valuesFromLine.GetEnumerator();
        }

        public string ReadFirstLine()
        {
            lineFromFile = ReadFileColumns();
            valuesFromLine = null;
            return lineFromFile;
        }

        public string ReadNextLine()
        {
            lineFromFile = NextLine();
            valuesFromLine = null;
            return lineFromFile;
        }
    }
}
