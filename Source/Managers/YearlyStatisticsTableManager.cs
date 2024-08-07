﻿using System.Windows.Controls;
using System.Windows.Data;

namespace WatchedFilmsTracker.Source.Managers
{
    public class YearlyStatisticsTableManager

    {
        private DataGrid dataGrid;
        private List<DataGridColumn> columns;

        public YearlyStatisticsTableManager(DataGrid dataGrid)
        {
            this.dataGrid = dataGrid;
            columns = new List<DataGridColumn>();
            InitializeColumns();

            dataGrid.CanUserAddRows = false;
            dataGrid.CanUserDeleteRows = false;
            dataGrid.IsReadOnly = true;
        }

        private void InitializeColumns()
        {
            columns.Add(new DataGridTextColumn { Header = "Year", Binding = new Binding("Year"), Width = new DataGridLength(1, DataGridLengthUnitType.Auto), IsReadOnly = true });
            columns.Add(new DataGridTextColumn { Header = "Number of films", Binding = new Binding("NumberOfFilms"), Width = new DataGridLength(1, DataGridLengthUnitType.Auto) });
            columns.Add(new DataGridTextColumn { Header = "Average rating", Binding = new Binding("AverageRatingString"), Width = new DataGridLength(1, DataGridLengthUnitType.Auto) });

            foreach (var column in columns)
            {
                dataGrid.Columns.Add(column);
            }
        }
    }
}