using System;
using System.Collections.Generic;
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

    public class RecordModel : INotifyPropertyChanged
    {
        public List<Cell> Cells
        {
            get => _cells;
            set
            {
                if (_cells != value)
                {
                    _cells = value;
                    NotifyPropertyChanged(nameof(Cells));
                }
            }
        }

        private List<Cell> _cells;

        public RecordModel(List<Cell> cells)
        {
            Cells = cells;
            foreach (var cell in Cells)
            {
                cell.PropertyChanged += Cell_PropertyChanged;
            }
        }

        public event EventHandler<CellChangedEventArgs> CellChanged;

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        public void AddNewCell()
        {
            Cell newCell = new Cell(string.Empty);
            Cells.Add(newCell);

            newCell.PropertyChanged += Cell_PropertyChanged;
        }

        public void InsertNewCellAt(int index)
        {
            Cell newCell = new Cell(string.Empty);
            Cells.Insert(index, newCell);

            newCell.PropertyChanged += Cell_PropertyChanged;
        }

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

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnCellChanged(CellChangedEventArgs e)
        {
            CellChanged?.Invoke(this, e);
            NotifyPropertyChanged(nameof(Cells)); // Notify that a change in a Cell has occurred
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