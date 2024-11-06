using System.IO;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.Csv;
using WatchedFilmsTracker.Source.ManagingFilmsFile;
using WatchedFilmsTracker.Source.ManagingRecords;

namespace WatchedFilmsTracker.Source.Services.Csv
{
    public class CSVreader
    {
        public Metadata Metadata;
        private List<DataGridTextColumn> _columns;
        private List<string> _listOfRecords;
        private int _maxColumns;
        private string fileColumns;
        private string filepath;
        private StreamReader filmsFile;
        private IEnumerator<string> iterator;
        private string lineFromFile;
        private List<string> valuesFromLine;

        public CSVreader(string filepath)
        {
            _maxColumns = 0;
            _columns = new List<DataGridTextColumn>();
            _listOfRecords = new List<string>();
            this.filepath = filepath;
        }

        public void CloseFile()
        {
            //  filmsFile.Close();
        }

        public List<DataGridTextColumn> GetColumns()
        {
            return _columns;
        }

        public string GetFileColumns()
        {
            return fileColumns;
        }

        public string GetFilePath()
        {
            return filepath;
        }

        public List<string> GetListOfRecords()
        {
            return _listOfRecords;
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

        public void ReadFile()
        {
            var lines = File.ReadAllLines(filepath);
            bool headersProcessed = false;
            bool commentsProcessed = false;
            var commentLines = new List<string>();

            foreach (var line in lines)
            {
                // read comments
                if (!commentsProcessed)
                {
                    if (line.StartsWith("#"))
                    {
                        commentLines.Add(line);
                        continue;
                    }
                    else
                    {
                        Metadata = new Metadata(commentLines);
                        commentsProcessed = true;
                    }
                }

                var values = line.Split('\t').Select(v => v.Trim()).ToList();

                // read headers
                if (!headersProcessed)
                {
                    foreach (var header in values)
                    {
                        _columns.Add(new DataGridTextColumn
                        {
                            Header = header,
                        });
                    }
                    _maxColumns = values.Count;
                    headersProcessed = true;
                    continue;
                }

                // read data
                _listOfRecords.Add(line);

                // Update max columns if needed
                int columnsInThisLine = line.Split('\t').Length;
                if (columnsInThisLine > _maxColumns)
                {
                    _maxColumns = columnsInThisLine;
                }
            }

        }
    }
}