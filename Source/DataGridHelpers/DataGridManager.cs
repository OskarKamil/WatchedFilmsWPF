using System.Windows.Controls;
using System.Windows.Data;

namespace WatchedFilmsTracker.Source.DataGridHelpers
{
    internal class DataGridManager
    {
        private readonly List<DataGridTextColumn> columns = new List<DataGridTextColumn>();
        private readonly DataGrid dataGrid;
        private readonly List<int> defaultOrder = new List<int>();
        private readonly List<double> defaultWidths = new List<double>();

        public DataGridManager(DataGrid dataGrid)
        {
            this.dataGrid = dataGrid;
        }

        public DataGridTextColumn AddColumn(string header)
        {
            var newColumn = new DataGridTextColumn { Header = header };
            dataGrid.Columns.Add(newColumn);
            return newColumn;
        }

        public void BuildColumnsFromList(List<DataGridTextColumn> columns)
        {
            // Ensure the DataGrid has no pre-existing columns
            dataGrid.Columns.Clear();

            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];

                var dataGridColumn = new DataGridTextColumn
                {
                    Header = column.Header,
                    // Bind to the corresponding index in the Values list of the data model
                    Binding = new Binding($"Cells[{i}].Value")
                };

                // Add the column to the DataGrid
                dataGrid.Columns.Add(dataGridColumn);
            }
        }

        public int GetIdOfColumnByHeader(string header)
        {
            int columnIndex = -1;
            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                if (dataGrid.Columns[i].Header.ToString().ToLower() == header.ToLower())
                {
                    columnIndex = i;
                    break;
                }
            }
            return columnIndex;
        }

        public int GetNumberOfColumns()
        {
            return dataGrid.Columns.Count;
        }

        public void ResetToDefault()
        {
            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                dataGrid.Columns[i].DisplayIndex = i;
                dataGrid.Columns[i].Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
            }
        }

        private void CreateDefaultColumnsForCollectionOfFilms()
        {
            columns.Add(new DataGridTextColumn { Header = "#", Binding = new Binding("IdInList"), Width = new DataGridLength(1, DataGridLengthUnitType.Auto), IsReadOnly = true });
            columns.Add(new DataGridTextColumn { Header = "English title", Binding = new Binding("EnglishTitle"), Width = 250 });
            columns.Add(new DataGridTextColumn { Header = "Original title", Binding = new Binding("OriginalTitle"), Width = 200 });
            columns.Add(new DataGridTextColumn { Header = "Type", Binding = new Binding("Type"), Width = 100 });
            columns.Add(new DataGridTextColumn { Header = "Release year", Binding = new Binding("ReleaseYear"), Width = 75 });
            columns.Add(new DataGridTextColumn { Header = "Rating", Binding = new Binding("Rating"), Width = 50 });
            columns.Add(new DataGridTextColumn { Header = "Watch date", Binding = new Binding("WatchDate"), Width = 80 });
            columns.Add(new DataGridTextColumn { Header = "Comments", Binding = new Binding("Comments"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });

            foreach (var column in columns)
            {
                dataGrid.Columns.Add(column);
                defaultWidths.Add(column.ActualWidth);
                defaultOrder.Add(columns.IndexOf(column));
            }
        }

        //private void RefreshColumns()
        //{
        //    dataGrid.Columns.Clear();

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

        //private void RemoveColumn(int columnIndex)
        //{
        //    if (columnIndex >= 0 && columnIndex < ColumnRepresentation.Count)
        //    {
        //        ColumnRepresentation[columnIndex] = -1; // Mark column as removed
        //        RefreshColumns();
        //    }
        //}

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