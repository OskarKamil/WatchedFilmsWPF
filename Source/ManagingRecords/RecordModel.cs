using System.ComponentModel;
using System.Diagnostics;
using WatchedFilmsTracker.Source.ManagingRecords;
using static WatchedFilmsTracker.Source.ManagingRecords.CellDataType;

namespace WatchedFilmsTracker.Source.ManagingFilmsFile
{
    public class CellValueChangedEventArgs : EventArgs
    {
        public Cell Cell { get; }
        public string PropertyName { get; }

        public CellValueChangedEventArgs(Cell cell, string propertyName)
        {
            Cell = cell;
            PropertyName = propertyName;
        }
    }

    public class RecordModel : INotifyPropertyChanged
    {
        public List<Cell> Cells
        {
            get => _cells;
            private set
            {
                if (_cells != value)
                {
                    _cells = value;
                    OnPropertyChanged(nameof(Cells));
                    foreach (var cell in _cells)
                    {
                        Debug.WriteLine("does it even work?");

                        cell.PropertyChanged += OnRecordModelChanged;
                    }
                }
            }
        }

        private List<Cell> _cells;

        public RecordModel(List<Cell> cells)
        {
            Cells = cells;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddNewCell()
        {
            Cell newCell = new Cell(string.Empty);
            newCell.DataType = DataType.Number;
            Cells.Add(newCell);

            newCell.PropertyChanged += OnRecordModelChanged;
        }

        public void AddNewCell(string text)
        {
            Cell newCell = new Cell(text);
            Cells.Add(newCell);

            newCell.PropertyChanged += OnRecordModelChanged;
        }

        public void InsertNewCellAt(int index)
        {
            Cell newCell = new Cell(string.Empty);
            Cells.Insert(index, newCell);

            newCell.PropertyChanged += OnRecordModelChanged;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnRecordModelChanged(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine("cell has been edited");
            if (sender is Cell cell)
            {
                if (e.PropertyName == "Value")
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e.PropertyName));
            }
        }
    }
}