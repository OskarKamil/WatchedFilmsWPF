using System.ComponentModel;
using WatchedFilmsTracker.Source.ManagingRecords;
using static WatchedFilmsTracker.Source.ManagingRecords.CellDataType;

namespace WatchedFilmsTracker.Source.ManagingFilmsFile
{
    /// <summary>
    /// Event arguments used when a <see cref="Cell"/> value or other property changes and
    /// the change is propagated from the <see cref="RecordModel"/>.
    /// </summary>
    public class CellValueChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The <see cref="Cell"/> instance that raised the change.
        /// </summary>
        public Cell Cell { get; }

        /// <summary>
        /// The name of the property that changed on the cell (for example, <c>Value</c>).
        /// </summary>
        public string PropertyName { get; }

        public CellValueChangedEventArgs(Cell cell, string propertyName)
        {
            Cell = cell;
            PropertyName = propertyName;
        }
    }

    /// <summary>
    /// Represents a single record composed of multiple <see cref="Cell"/> instances.
    /// The model exposes change notifications both via <see cref="INotifyPropertyChanged"/>
    /// and the <see cref="CellValueChanged"/> event which bubbles up cell-level changes.
    /// </summary>
    public class RecordModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Raised when a property on one of the child <see cref="Cell"/> instances changes.
        /// Subscribers receive the cell that changed and the property name via <see cref="CellValueChangedEventArgs"/>.
        /// </summary>
        public event EventHandler<CellValueChangedEventArgs>? CellValueChanged;

        private List<Cell> _cells = new List<Cell>();

        /// <summary>
        /// The list of cells for this record. Subscribes to each cell's PropertyChanged
        /// so the model can bubble up relevant notifications.
        /// </summary>
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

                // assign the new collection (use empty list if null)
                _cells = value ?? new List<Cell>();

                // Subscribe handlers to the new collection
                foreach (var cell in _cells)
                    cell.PropertyChanged += OnRecordModelChanged;

                // Notify bindings that the collection changed
                OnPropertyChanged(nameof(Cells));
            }
        }

        /// <summary>
        /// Creates a new <see cref="RecordModel"/> with the provided initial cell list.
        /// </summary>
        /// <param name="cells">Initial list of cells. If null, an empty list is used.</param>
        public RecordModel(List<Cell> cells)
        {
            Cells = cells ?? new List<Cell>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Adds a new empty cell with the specified data type to the end of the cell list.
        /// </summary>
        /// <param name="dataType">The data type to assign to the newly created cell.</param>
        public void AddNewCell(DataType dataType)
        {
            if (dataType == null)
                dataType = DataType.String; // Default to string if null is passed
            var newCell = new Cell(string.Empty) { DataType = dataType };
            Cells.Add(newCell);
            newCell.PropertyChanged += OnRecordModelChanged;
            // Notify bindings that the collection changed
            OnPropertyChanged(nameof(Cells));
        }

        /// <summary>
        /// Adds a new cell with the provided text value to the end of the cell list.
        /// </summary>
        /// <param name="text">Initial text value of the new cell.</param>
        public void AddNewCell(string text)
        {
            Cell newCell = new Cell(text);
            Cells.Add(newCell);
            newCell.PropertyChanged += OnRecordModelChanged;
            OnPropertyChanged(nameof(Cells));
        }

        /// <summary>
        /// Inserts a new empty cell at the specified index.
        /// </summary>
        /// <param name="index">Zero-based index at which to insert the new cell.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when index is outside the valid range.</exception>
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

            // Bubble up only when the cell's Value property changes. Use nameof to avoid magic strings.
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