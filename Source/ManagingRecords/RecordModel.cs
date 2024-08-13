using System.ComponentModel;
using WatchedFilmsTracker.Source.DataGridHelpers;

namespace WatchedFilmsTracker.Source.ManagingFilmsFile
{
    public class CellChangedEventArgs : EventArgs
    {
        public Cell Cell { get; }
        public string PropertyName { get; }

        public CellChangedEventArgs(Cell cell, string propertyName)
        {
            Cell = cell;
            PropertyName = propertyName;
        }
    }

    public class RecordModel
    {
        public List<Cell> Cells { get; set; } = new List<Cell>();

        public RecordModel(List<Cell> cells)
        {
            Cells = cells;
            foreach (var cell in Cells)
            {
                cell.PropertyChanged += Cell_PropertyChanged;
            }
        }

        public event EventHandler<CellChangedEventArgs> CellChanged;

        public string StringToBeSavedInFile()
        {
            return "wip";
            //todo check which columns exist and what order
        }

        public string ToNewString()
        {
            return "wip";
            //todo all
        }

        protected virtual void OnCellChanged(CellChangedEventArgs e)
        {
            CellChanged?.Invoke(this, e);
        }

        private void Cell_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Cell cell)
            {
                OnCellChanged(new CellChangedEventArgs(cell, e.PropertyName));
            }
        }
    }
}