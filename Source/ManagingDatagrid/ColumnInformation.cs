using System.Windows.Controls;
using WatchedFilmsTracker.Source.ManagingRecords;

namespace WatchedFilmsTracker.Source.ManagingDatagrid
{
    internal class ColumnInformation
    {
        public DataGridTextColumn DataGridTextColumn { get; set; }
        public DataType DataType { get; set; }

        public ColumnInformation(DataGridTextColumn dataGridTextColumn, DataType dataType)
        {
            DataGridTextColumn = dataGridTextColumn;
            DataType = dataType;
        }
    }
}