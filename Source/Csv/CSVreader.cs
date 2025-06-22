using System.IO;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.Csv;

namespace WatchedFilmsTracker.Source.Services.Csv
{
    public class CSVreader
    {
        public Metadata Metadata;
        private List<DataGridTextColumn> _columns;
        private List<string> _listOfRecords;
        private int _maxColumns;
        private string filepath;

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

        public List<string> GetListOfRecords()
        {
            return _listOfRecords;
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

                // todo rectancugalize data
                // read all data
                //  keep track of the longest row
                // rectangularize data
                // store column headers

                // outside of this method
                // create columns
                // populate cells, each population, check column data

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