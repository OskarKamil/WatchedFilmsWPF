using System.ComponentModel;
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
        public event EventHandler<CellValueChangedEventArgs>? CellValueChanged;

        private List<Cell> _cells = new List<Cell>();

        public List<Cell> Cells
        {
            get => _cells;
            private set
            {
                if (_cells == value)
                    return;

                // Unsubscribe handlers from the old collection to avoid memory leaks / duplicate handlers
                if (_cells != null)
                {
                    foreach (var oldCell in _cells)
                        oldCell.PropertyChanged -= OnRecordModelChanged;
                }

                foreach (var cell in _cells)
                    cell.PropertyChanged += OnRecordModelChanged;

                OnPropertyChanged(nameof(Cells));
            }
        }

        public RecordModel(List<Cell> cells)
        {
            Cells = cells ?? new List<Cell>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddNewCell(DataType dataType)
        {
            if (dataType == null)
                dataType = DataType.String;
            var newCell = new Cell(string.Empty) { DataType = dataType };
            Cells.Add(newCell);
            newCell.PropertyChanged += OnRecordModelChanged;
            OnPropertyChanged(nameof(Cells));
        }

        public void AddNewCell(string text)
        {
            Cell newCell = new Cell(text);
            Cells.Add(newCell);
            newCell.PropertyChanged += OnRecordModelChanged;
            OnPropertyChanged(nameof(Cells));
        }

        public void InsertNewCellAt(int index)
        {
            if (index < 0 || index > Cells.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            Cell newCell = new Cell(string.Empty);
            Cells.Insert(index, newCell);
            newCell.PropertyChanged += OnRecordModelChanged;
            OnPropertyChanged(nameof(Cells));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnRecordModelChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not Cell cell)
                return;

            if (e.PropertyName == nameof(Cell.Value))
            {
                // First raise a dedicated event so callers can get the specific cell that changed.
                CellValueChanged?.Invoke(this, new CellValueChangedEventArgs(cell, e.PropertyName));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e.PropertyName));

                // Also raise PropertyChanged for Cells so any bindings to the collection can update.
                OnPropertyChanged(nameof(Cells));
            }
        }
    }
}