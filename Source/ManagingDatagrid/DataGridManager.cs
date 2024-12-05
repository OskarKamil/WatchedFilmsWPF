using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Data;
using WatchedFilmsTracker.Source.ManagingDatagrid;
using static WatchedFilmsTracker.Source.ManagingRecords.CellDataType;

namespace WatchedFilmsTracker.Source.DataGridHelpers
{
    internal class DataGridManager
    {
        public ObservableCollection<ColumnInformation> ColumnsAndDataTypes { get; } = new ObservableCollection<ColumnInformation>();
        public DataGrid DataGrid { get; set; }
        private readonly List<int> defaultOrder = new List<int>();
        private readonly List<double> defaultWidths = new List<double>();

        public DataGridManager(DataGrid dataGrid)
        {
            DataGrid = dataGrid;
        }

        public DataGridTextColumn AddColumn(string header)
        {
            var newColumn = new DataGridTextColumn { Header = header };
            DataGrid.Columns.Add(newColumn);
            ColumnsAndDataTypes.Add(new ColumnInformation(newColumn, DataType.String));
            return newColumn;
        }

        //    for (int i = 0; i < ColumnRepresentation.Count; i++)
        //    {
        //        if (ColumnRepresentation[i] != -1)
        //        {
        //            var binding = new Binding($"Properties[{ColumnRepresentation[i]}]");
        //            var column = new DataGridTextColumn
        //            {
        //                Header = $"Column {i + 1}",
        //                Binding = binding
        //            };
        //            dataGrid.Columns.Add(column);
        //        }
        //    }
        //}
        public DataGridTextColumn AddColumnAtIndex(int index, string header)
        {
            var newColumn = new DataGridTextColumn { Header = header };
            DataGrid.Columns.Insert(index, newColumn);
            ColumnsAndDataTypes.Insert(index, new ColumnInformation(newColumn, DataType.String));
            return newColumn;
        }

        public void BuildColumnsFromList(List<DataGridTextColumn> columns)
        {
            // Ensure the DataGrid has no pre-existing columns
            DataGrid.Columns.Clear();

            for (int i = 0; i < columns.Count; i++)
            {
                var dataGridColumn = AddColumn(columns[i].Header.ToString());
                dataGridColumn.Binding = new Binding($"Cells[{i}].Value");
            }
        }

        public DataGridColumn GetColumnByDisplayIndex(int columnDisplayIndex)
        {
            return DataGrid.Columns.FirstOrDefault(col => col.DisplayIndex == columnDisplayIndex);
        }

        public int GetColumnIdByDisplayIndex(int columnDisplayIndex)
        {
            var column = DataGrid.Columns.FirstOrDefault(col => col.DisplayIndex == columnDisplayIndex);
            return column != null ? DataGrid.Columns.IndexOf(column) : -1;
        }

        public int GetIdOfColumnByHeader(string header)
        {
            int columnIndex = -1;
            for (int i = 0; i < DataGrid.Columns.Count; i++)
            {
                if (DataGrid.Columns[i].Header.ToString().ToLower() == header.ToLower())
                {
                    columnIndex = i;
                    break;
                }
            }

            return columnIndex;
        }

        public int GetNumberOfColumns()
        {
            return DataGrid.Columns.Count;
        }

        //private void RefreshColumns()
        //{
        //    dataGrid.Columns.Clear();
        public void RemoveColumnAt(int columnIndex)
        {
            if (columnIndex >= 0 && columnIndex < DataGrid.Columns.Count)
            {
                DataGrid.Columns.RemoveAt(columnIndex);
                ColumnsAndDataTypes.RemoveAt(columnIndex);
            }
        }

        public void RenameColumnAt(int columnIndex, string header)
        {
            DataGrid.Columns[columnIndex].Header = header;
            ColumnsAndDataTypes[columnIndex].DataGridTextColumn.Header = header;
            ColumnsAndDataTypes[columnIndex].OnPropertyChanged(nameof(ColumnInformation.DisplayText));
        }

        public void ResetToDefault()
        {
            for (int i = 0; i < DataGrid.Columns.Count; i++)
            {
                DataGrid.Columns[i].DisplayIndex = i;
                DataGrid.Columns[i].Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
            }
        }

        //private void SwapColumns(int index1, int index2)
        //{
        //    if (index1 >= 0 && index1 < ColumnRepresentation.Count &&
        //        index2 >= 0 && index2 < ColumnRepresentation.Count)
        //    {
        //        var temp = ColumnRepresentation[index1];
        //        ColumnRepresentation[index1] = ColumnRepresentation[index2];
        //        ColumnRepresentation[index2] = temp;
    }
}