using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.DataGridHelpers;
using static WatchedFilmsTracker.Source.ManagingRecords.CellDataType;

namespace WatchedFilmsTracker.Source.ManagingDatagrid
{
    public class ColumnInformation : INotifyPropertyChanged
    {
        public DataGridManager DataGridManager { get; }

        private DataType _selectedDataType;
        public DataType SelectedDataType
        {
            get => _selectedDataType;
            set
            {
                if (_selectedDataType != value)
                {

                    _selectedDataType = value;
                    OnPropertyChanged(nameof(SelectedDataType));
                }
            }
        }

        public DataGridTextColumn DataGridTextColumn
        {
            get => _dataGridTextColumn;
            set
            {
                _dataGridTextColumn = value;
                OnPropertyChanged(nameof(DataGridTextColumn));
            }
        }

        public DataType DataType
        {
            get => _dataType;
            set
            {
                if (_dataType != value)
                {
                    _dataType = value;
                    SelectedDataType = _dataType;
                    OnPropertyChanged(nameof(DataType));
                }
            }
        }

        public String DisplayText => $"{DataGridTextColumn.Header} - {DataType}";
        public Guid UniqueId { get; }
        private DataGridTextColumn _dataGridTextColumn;
        private DataType _dataType;

        public ColumnInformation(DataGridTextColumn dataGridTextColumn, DataType dataType, DataGridManager dataGridManager)
        {
            DataGridTextColumn = dataGridTextColumn;
            _dataType = dataType;
            SelectedDataType = dataType;
            DataGridManager = dataGridManager;
            UniqueId = Guid.NewGuid();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{DataGridTextColumn.Header} - {DataType}";
        }
    }
}