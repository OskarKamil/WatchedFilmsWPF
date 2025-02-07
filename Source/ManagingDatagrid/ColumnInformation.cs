using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using static WatchedFilmsTracker.Source.ManagingRecords.CellDataType;

namespace WatchedFilmsTracker.Source.ManagingDatagrid
{
    public class ColumnInformation : INotifyPropertyChanged
    {
        public DataGridTextColumn DataGridTextColumn
        {
            get => _dataGridTextColumn;
            set
            {
                _dataGridTextColumn = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DataGridTextColumn));
            }
        }

        public DataType DataType
        {
            get => _dataType;
            set
            {
                _dataType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DataType));
            }
        }

        public String DisplayText => $"{DataGridTextColumn.Header} - {DataType}";
        private DataGridTextColumn _dataGridTextColumn;
        private DataType _dataType;

        public ColumnInformation(DataGridTextColumn dataGridTextColumn, DataType dataType)
        {
            _dataGridTextColumn = dataGridTextColumn;
            _dataType = dataType;
        }

        public override string ToString()
        {
            return $"{DataGridTextColumn.Header} - {DataType}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
