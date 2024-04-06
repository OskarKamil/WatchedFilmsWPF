using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WatchedFilmsTracker.Source.Managers
{
    internal class ColumnsManager
    {
        private List<DataGridColumn> columns = new List<DataGridColumn>();
        private List<DataGridColumn> defaultColumns = new List<DataGridColumn>();

        public ColumnsManager(DataGrid dataGrid)
        {
            foreach (var column in dataGrid.Columns)
            {
                columns.Add(column);
                defaultColumns.Add(CloneColumn(column));
            }
        }

        private DataGridColumn CloneColumn(DataGridColumn column)
        {
            if (column is DataGridColumn textColumn)
            {
                DataGridColumn clonedColumn = new DataGridTextColumn
                {
                    Header = textColumn.Header,
                }; return clonedColumn;
            }
            return null;
        }

        public void ResetToDefault()
        {
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var defaultColumn = defaultColumns[i];

                column.DisplayIndex = i;

                // Restore width
                column.Width = 100;
                column.Width = DataGridLength.Auto;
                if (column.Header == "Comments")
                    column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            }

        }
    }
}