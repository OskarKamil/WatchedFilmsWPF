using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Services.Csv
{
    public class CSVreader
    {
        private List<Column> _columns;
        private int _maxColumns;
        private ObservableCollection<FilmRecord> _records;
        private string fileColumns;
        private string filePath;
        private StreamReader filmsFile;
        private IEnumerator<string> iterator;
        private string lineFromFile;
        private List<string> valuesFromLine;

        public CSVreader()
        {
            _maxColumns = 0;
            _columns = new List<Column>();
            _records = new ObservableCollection<FilmRecord>();
        }

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
            Debug.WriteLine("");
        }

        public void CloseFile()
        {
            //filmsFile.Close();
        }

        public List<Column> GetColumns()
        {
            return _columns;
        }

        public string GetFileColumns()
        {
            return fileColumns;
        }

        public string GetFilePath()
        {
            return filePath;
        }

             public ObservableCollection<FilmRecord> GetRecords()
        {
            return _records;
        }

        public bool HasNextLine()
        {
            return !filmsFile.EndOfStream;
        }

        public string NextLine()
        {
            if (HasNextLine())
                return filmsFile.ReadLine();
            else
                return "";
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

        public ObservableCollection<FilmRecord> ReadCsvReturnObservableCollection(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            bool headersProcessed = false;

            foreach (var line in lines)
            {
                // Split the line into values by tabs and trim whitespace
                var values = line.Split('\t').Select(v => v.Trim()).ToList();

                if (!headersProcessed)
                {
                    // Create columns from the first line
                    foreach (var header in values)
                    {
                        _columns.Add(new Column(header));
                    }
                    _maxColumns = values.Count;
                    headersProcessed = true;
                    continue;
                }

                // If the current line has more values than existing columns, add new columns
                while (values.Count > _columns.Count)
                {
                    _columns.Add(new Column($"Column{_columns.Count + 1}"));
                }

                // Create a list of Cell objects
                var cells = new List<Cell>();

                for (int i = 0; i < _columns.Count; i++)
                {
                    string cellValue = i < values.Count ? values[i] : string.Empty;
                    cells.Add(new Cell(cellValue, _columns[i]));
                }

                // Create a new FilmRecord with the populated list
                var record = new FilmRecord(cells);
                _records.Add(record);

                // Update max columns if needed
                if (values.Count > _maxColumns)
                {
                    _maxColumns = values.Count;
                }
            }

            FillEmptyValues();

            // Return an ObservableCollection of FilmRecord
            return new ObservableCollection<FilmRecord>(_records);
        }

        public string ReadFirstLine()
        {
            lineFromFile = ReadCSVheaders();
            valuesFromLine = null;
            return lineFromFile;
        }

        public string ReadNextLine()
        {
            lineFromFile = NextLine();
            valuesFromLine = null;
            return lineFromFile;
        }

        private void FillEmptyValues()
        {
            // Ensure all FilmRecords have the same number of Cells
            foreach (var record in _records)
            {
                while (record.Cells.Count < _columns.Count)
                {
                    record.Cells.Add(new Cell(string.Empty, _columns[record.Cells.Count]));
                }
            }
        }
        private string ReadCSVheaders()
        {
            fileColumns = NextLine();
            return fileColumns;
        }
    }
}