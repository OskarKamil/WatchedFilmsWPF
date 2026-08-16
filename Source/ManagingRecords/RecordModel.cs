using System.ComponentModel;
using WatchedFilmsTracker.Source.ManagingRecords;
using static WatchedFilmsTracker.Source.ManagingRecords.CellDataType;

namespace WatchedFilmsTracker.Source.ManagingFilmsFile
{
    public class RecordModel : INotifyPropertyChanged
    {
        public List<Cell> Cells { get; }

        public RecordModel(List<Cell> cells)
        {
            Cells = cells ?? new List<Cell>();

            foreach (Cell cell in Cells)
                cell.PropertyChanged += Cell_PropertyChanged;
        }

        public event EventHandler? CellValueChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void AddNewCell(DataType dataType)
        {
            var newCell = new Cell(string.Empty) { DataType = dataType };
            Cells.Add(newCell);
            newCell.PropertyChanged += Cell_PropertyChanged;
            OnPropertyChanged(nameof(Cells));
        }

        public void AddNewCell(string text)
        {
            Cell newCell = new Cell(text);
            Cells.Add(newCell);
            newCell.PropertyChanged += Cell_PropertyChanged;
            OnPropertyChanged(nameof(Cells));
        }

        public void InsertNewCellAt(int index)
        {
            if (index < 0 || index > Cells.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            Cell newCell = new Cell(string.Empty);
            Cells.Insert(index, newCell);
            newCell.PropertyChanged += Cell_PropertyChanged;
            OnPropertyChanged(nameof(Cells));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Cell_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Cell && e.PropertyName == nameof(Cell.Value))
                CellValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
