using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;
using WatchedFilmsTracker.Source.ManagingDatagrid;
using static WatchedFilmsTracker.Source.ManagingRecords.CellDataType;

namespace WatchedFilmsTracker.Source.DataGridHelpers
{
    public class DataGridManager : INotifyPropertyChanged
    {
        public IEnumerable<DataType> AllDataTypes => GetValues();
        public ObservableCollection<ColumnInformation> ColumnsAndDataTypes { get; } = new();

        public DataGrid DataGrid { get; set; }
        private readonly List<int> defaultOrder = new List<int>();
        private readonly List<double> defaultWidths = new List<double>();

        public DataGridManager(DataGrid dataGrid)
        {
            DataGrid = dataGrid;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public DataGridTextColumn AddColumn(string header)
        {
            var newColumn = new DataGridTextColumn { Header = header };
            DataGrid.Columns.Add(newColumn);
            ColumnsAndDataTypes.Add(new ColumnInformation(newColumn, DataType.String, this)); // "this" passed explicitly
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
        public ColumnInformation AddColumnAtIndex(int index, string header)
        {
            var newColumn = new DataGridTextColumn { Header = header };
            DataGrid.Columns.Insert(index, newColumn);
            var newColumnInformation = new ColumnInformation(newColumn, DataType.String, this);
            ColumnsAndDataTypes.Insert(index, newColumnInformation);
            return newColumnInformation;
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

        public void ChangeDataTypeOfColumn(int columnIndex, DataType dataType)
        {
            ColumnsAndDataTypes[columnIndex].DataType = dataType;
        }

        public void ChangeDataTypeOfColumn(ColumnInformation columnInformation, DataType dataType)
        {
            int index = ColumnsAndDataTypes.IndexOf(columnInformation);
            if (index != -1)
            {
                ColumnsAndDataTypes[index].DataType = dataType;
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

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
                                                                                                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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