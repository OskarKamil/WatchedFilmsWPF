using System.IO;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.Csv;

namespace WatchedFilmsTracker.Source.Services.Csv
{
    public class CSVreader
    {
        public Metadata Metadata;
        private List<DataGridTextColumn> _columns;
        private List<List<string>> _listOfRecords;
        private int _maxColumns;
        private string filepath;

        public CSVreader(string filepath)
        {
            _maxColumns = 0;
            _columns = new List<DataGridTextColumn>();
            _listOfRecords = new List<List<string>>();
            this.filepath = filepath;
        }

        public List<DataGridTextColumn> GetColumns()
        {
            return _columns;
        }

        public List<List<string>> GetListOfRecords()
        {
            return _listOfRecords;
        }

        public void NormalizeData(List<List<String>> recordsList)
        {
            // generate missing columns
            int columnsToAdd = _maxColumns - _columns.Count;
            for (int i = 0; i < columnsToAdd; i++)
            {
                _columns.Add(new DataGridTextColumn
                {
                    Header = "",
                });
            }

            foreach (var record in recordsList)
            {
                int valuesToAdd = _maxColumns - record.Count;
                for (int i = 0; i < valuesToAdd; i++)
                {
                    record.Add("");
                }
            }
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
                _listOfRecords.Add(values);

                // Update max columns if needed

                if (values.Count > _maxColumns)
                {
                    _maxColumns = values.Count;
                }
            }
            NormalizeData(_listOfRecords);
        }
    }
}