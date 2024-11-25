using System.Windows.Controls;
using WatchedFilmsTracker.Source.ManagingRecords;
using static WatchedFilmsTracker.Source.ManagingRecords.CellDataType;

namespace WatchedFilmsTracker.Source.ManagingDatagrid
{
    internal class ColumnInformation
    {
        public DataGridTextColumn DataGridTextColumn { get; set; }
        public DataType DataType { get; set; }
        public String DisplayText => $"{DataGridTextColumn.Header} - {DataType}";

        public ColumnInformation(DataGridTextColumn dataGridTextColumn, DataType dataType)
        {
            DataGridTextColumn = dataGridTextColumn;
            DataType = dataType;
        }
    }
}