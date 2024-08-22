﻿using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.ManagingFilmsFile;

namespace WatchedFilmsTracker.Source.Services.Csv
{
    public class CSVreader
    {
        private List<DataGridTextColumn> _columns;
        private int _maxColumns;
        private ObservableCollection<RecordModel> _records;
        private string fileColumns;
        private string filePath;
        private StreamReader filmsFile;
        private IEnumerator<string> iterator;
        private string lineFromFile;
        private List<string> valuesFromLine;

        public CSVreader()
        {
            _maxColumns = 0;
            _columns = new List<DataGridTextColumn>();
            _records = new ObservableCollection<RecordModel>();
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
            return filePath;
        }

        public ObservableCollection<RecordModel> GetRecords()
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

        public ObservableCollection<RecordModel> ReadCsvReturnObservableCollection(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            bool headersProcessed = false;

            foreach (var line in lines)
            {
                var values = line.Split('\t').Select(v => v.Trim()).ToList();

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

                var cells = new List<Cell>();

                for (int i = 0; i < _columns.Count; i++)
                {
                    string cellValue = i < values.Count ? values[i] : string.Empty;
                    cells.Add(new Cell(cellValue));
                }

                // Create a new FilmRecord with the populated list
                var record = new RecordModel(cells);
                _records.Add(record);

                // Update max columns if needed
                if (values.Count > _maxColumns)
                {
                    _maxColumns = values.Count;
                }
            }

            FillEmptyValues();

            // Return an ObservableCollection of FilmRecord
            return new ObservableCollection<RecordModel>(_records);
        }

        private void FillEmptyValues()
        {
            // Ensure all FilmRecords have the same number of Cells
            foreach (var record in _records)
            {
                while (record.Cells.Count < _columns.Count)
                {
                    record.Cells.Add(new Cell(string.Empty));
                }
            }
        }
    }
}