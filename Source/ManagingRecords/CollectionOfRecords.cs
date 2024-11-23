using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.ManagingRecords;
using WatchedFilmsTracker.Source.Views;

namespace WatchedFilmsTracker.Source.ManagingFilmsFile
{
    public class CollectionOfRecords
    {
        public ObservableCollection<RecordModel> ObservableCollectionOfRecords { get; set; }
        public List<DataGridTextColumn> Columns;
        internal DataGridManager DataGridManager { get; set; }
        private string fileColumnHeaders;
        private WorkingTextFile workingTextFile;

        public CollectionOfRecords(WorkingTextFile filmsTextFile)
        {
            this.workingTextFile = filmsTextFile;

            ObservableCollectionOfRecords = new ObservableCollection<RecordModel>();
        }

        public event EventHandler<EventArgs> AnyRecordHasChanged;

        public event PropertyChangedEventHandler PropertyChanged;
        public void AddEmptyRecordToList()
        {
            RecordModel newRecord = new RecordModel(new List<Cell>());

            for (int i = 0; i < DataGridManager.DataGrid.Columns.Count; i++)
            {
                {
                    newRecord.AddNewCell();
                }
            }

            // fill in # id column
            int indexOfColumnID = DataGridManager.GetIdOfColumnByHeader("#");
            if (indexOfColumnID != -1)
            {
                newRecord.Cells[indexOfColumnID].Value = (ObservableCollectionOfRecords.Count + 1).ToString();
            }

            ObservableCollectionOfRecords.Add(newRecord);
            workingTextFile.DataGrid.SelectedCells.Clear();

            if (workingTextFile.DataGrid.ItemsSource == ObservableCollectionOfRecords)
            {
                workingTextFile.DataGrid.SelectedItem = newRecord;
                workingTextFile.DataGrid.ScrollIntoView(workingTextFile.DataGrid.SelectedItem);
            }

            if (SettingsManager.DefaultDateIsToday)
            {
                string formattedString = DateTime.Now.ToString("dd/MM/yyyy");
                int columnID = DataGridManager.GetIdOfColumnByHeader("Watch date");
                if (columnID != -1)
                    newRecord.Cells[columnID].Value = formattedString;
            }

            newRecord.PropertyChanged += HandleRecordCellTextHasChanged;

            StartEditingRecord(newRecord);

            workingTextFile.AnyChangeHappen();
        }

        public void AddRecordFromText(string text, string delimiter)
        {
            RecordModel newRecord = new RecordModel(new List<Cell>());
            List<string> values = text.Split(delimiter).ToList();

            //todo check why need to use columns.count instead of DataGridManager.DataGrid.Columns.Count
            for (int i = 0; i < Columns.Count; i++)
            {
                newRecord.AddNewCell(values[i]);
            }

            //int indexOfColumnID = DataGridManager.GetIdOfColumnByHeader("#");
            //if (indexOfColumnID != -1)
            //    newRecord.Cells[indexOfColumnID].NumberValue = (ObservableCollectionOfRecords.Count + 1);

            ObservableCollectionOfRecords.Add(newRecord);

            newRecord.PropertyChanged += HandleRecordCellTextHasChanged;
        }

        public DataGridTextColumn CreateColumnsWithIds()
        {
            var newColumn = CreateNewColumnAtIndex(0, "#");
            newColumn.DisplayIndex = 0;
            newColumn.IsReadOnly = true;
            newColumn.CanUserReorder = false;

            newColumn.Binding = new Binding($"Cells[{0}].Value");
            newColumn.SortMemberPath = ($"Cells[{0}].ComparableValue");

            for (int i = 0; i < ObservableCollectionOfRecords.Count; i++)
            {
                ObservableCollectionOfRecords[i].Cells[0].Value = (i + 1).ToString();
                ObservableCollectionOfRecords[i].Cells[0].CellDataType = DataType.Number;
            }
            workingTextFile.UnsavedChanges = false;
            return newColumn;
        }

        public void CreateDefaultColumnsForCommonCollectionType()
        {
            foreach (string columnHeader in workingTextFile.CommonCollectionType.DefaultColumnHeaders)
            {
                CreateNewColumn(columnHeader);
            }
        }

        public DataGridTextColumn CreateNewColumn(string columnHeader)
        {
            // todo datagrid manager should have columnsdatatypes, and each time column is inserted in the middle, it will update cells
            var column = DataGridManager.AddColumn(columnHeader);
            foreach (var RecordModel in ObservableCollectionOfRecords)
            {
                RecordModel.AddNewCell();
            }

            int indexOfNewCell = DataGridManager.DataGrid.Columns.Count - 1;

            column.Binding = new Binding($"Cells[{indexOfNewCell}].Value");

            workingTextFile.UnsavedChanges = true;
            workingTextFile.AnyChangeHappen();

            return column;
        }

        public DataGridTextColumn CreateNewColumnAtIndex(int index, string columnHeader)
        {
            var column = DataGridManager.AddColumnAtIndex(index, columnHeader);
            foreach (var RecordModel in ObservableCollectionOfRecords)
            {
                RecordModel.InsertNewCellAt(index);
            }
            column.Binding = new Binding($"Cells[{index}].Value");

            workingTextFile.UnsavedChanges = true;
            ShiftBindingAfterInsertion(0);
            return column;
        }

        public void DeleteAllRecords()
        {
            workingTextFile.CollectionOfRecords.ObservableCollectionOfRecords.Clear();
            workingTextFile.AnyChangeHappen();
        }

        public void DeleteColumn()
        {
            int columnID;
            var selectedCells = DataGridManager.DataGrid.SelectedCells;

            if (selectedCells.Count > 0)
            {
                var firstSelectedColumn = selectedCells.Select(sc => sc.Column).FirstOrDefault();
                columnID = DataGridManager.DataGrid.Columns.IndexOf(firstSelectedColumn);
                workingTextFile.AnyChangeHappen();
            }
            else
            {
                Debug.WriteLine("no selected cells");
                return;
            }

            DeleteColumn(columnID);
        }

        public void DeleteColumn(int columndID)
        {
            foreach (RecordModel recordModel in ObservableCollectionOfRecords)
            {
                recordModel.Cells.RemoveAt(columndID);
            }
            DataGridManager.DataGrid.Columns.RemoveAt(columndID);
            for (int i = columndID; i < DataGridManager.DataGrid.Columns.Count; i++)
            {
                // Cast the column to DataGridBoundColumn or DataGridTextColumn
                if (DataGridManager.DataGrid.Columns[i] is DataGridBoundColumn boundColumn)
                {
                    // Update the binding to refer to the correct cell index after a column is removed
                    boundColumn.Binding = new Binding($"Cells[{i}].Value");
                }
            }
            workingTextFile.UnsavedChanges = true;
            workingTextFile.AnyChangeHappen();
        }

        public void DeleteRecordFromList(RecordModel selected)
        {
            if (selected == null) { return; }

            int selectedIndex = ObservableCollectionOfRecords.IndexOf(selected);

            if (ObservableCollectionOfRecords.Count == 0 || selectedIndex < 0) return;

            int indexOfColumnID = DataGridManager.GetIdOfColumnByHeader("#");
            if (indexOfColumnID != -1)
            {
                int selectedIdColumnValue = int.Parse(selected.Cells[indexOfColumnID].Value);
                ObservableCollectionOfRecords.Remove(selected);
                RefreshFurtherIDs(selectedIdColumnValue);
            }
            else
            {
                ObservableCollectionOfRecords.Remove(selected);
            }

            // selecting the next record
            workingTextFile.DataGrid.SelectedCells.Clear();
            if (selectedIndex + 0 == workingTextFile.DataGrid.Items.Count)
            {
                workingTextFile.DataGrid.SelectedIndex = selectedIndex - 1;
            }
            else
            {
                workingTextFile.DataGrid.SelectedIndex = selectedIndex - 0;
            }
            workingTextFile.AnyChangeHappen();
        }

        public void PopulateListWithData(List<string> list, string delimiter)
        {
            foreach (string line in list)
            {
                AddRecordFromText(line, delimiter);
            }
        }

        public void RefreshFurtherIDs(int idOfSelected)
        {
            int indexOfColumnID = DataGridManager.GetIdOfColumnByHeader("#");
            if (indexOfColumnID != -1)
            {
                for (int i = 0; i < ObservableCollectionOfRecords.Count; i++)
                {
                    RecordModel record = ObservableCollectionOfRecords[i];
                    if (idOfSelected <= int.Parse(record.Cells[indexOfColumnID].Value))
                    {
                        int currentIndex = int.Parse(record.Cells[indexOfColumnID].Value);
                        record.Cells[indexOfColumnID].Value = (currentIndex - 1).ToString();
                    }
                }
            }
        }

        internal void RenameColumn()
        {
            int columnID;

            var selectedCells = DataGridManager.DataGrid.SelectedCells;

            if (selectedCells.Count > 0)
            {
                var firstSelectedColumn = selectedCells.Select(sc => sc.Column).FirstOrDefault();
                columnID = DataGridManager.DataGrid.Columns.IndexOf(firstSelectedColumn);
                var RenameColumnDialog = new RenameColumnDialog
                {
                    NewColumnName = firstSelectedColumn.Header.ToString()
                };
                RenameColumnDialog.Owner = System.Windows.Application.Current.MainWindow;
                RenameColumnDialog.ShowDialog();
                if (RenameColumnDialog.Result == Views.RenameColumnDialog.CustomDialogResult.Confirm)
                {
                    firstSelectedColumn.Header = RenameColumnDialog.NewColumnName;
                    workingTextFile.UnsavedChanges = true;
                }
            }
            else
            {
                Debug.WriteLine("no selected cells");
                return;
            }
        }

        protected virtual void HandleRecordCellTextHasChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("Record cell has changed, INVOKED FROM COLLECTION OF RECORDS");
            OnAnyRecordHasChanged(this, e);
            return;
        }

        protected virtual void OnAnyRecordHasChanged(object sender, EventArgs e)
        {
            AnyRecordHasChanged?.Invoke(this, e);
        }

        private void AdjustColumnsRepresentation(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            return;
        }

        private void ShiftBindingAfterDeletion(int index)
        {
            for (int i = 1; i < DataGridManager.DataGrid.Columns.Count - 1; i++)
            {
                ((DataGridTextColumn)DataGridManager.DataGrid.Columns[i]).Binding = new Binding($"Cells[{i}].Value");
            }
        }

        private void ShiftBindingAfterInsertion(int index)
        {
            for (int i = index + 1; i < DataGridManager.DataGrid.Columns.Count; i++)
            {
                ((DataGridTextColumn)DataGridManager.DataGrid.Columns[i]).Binding = new Binding($"Cells[{i}].Value");
            }
        }

        private void StartEditingRecord(RecordModel recordToEdit)
        {
            Debug.WriteLine("editing new record started");
            if (DataGridManager.GetNumberOfColumns() < 2)
                return;

            int columnIndexToEdit = DataGridManager.GetColumnIdByDisplayIndex(1);

            if (columnIndexToEdit > 0)
            {
                workingTextFile.DataGrid.CurrentCell = new DataGridCellInfo(recordToEdit, workingTextFile.DataGrid.Columns[columnIndexToEdit]);
                workingTextFile.DataGrid.BeginEdit();
            }
            else
            {
                return;
            }
        }
    }
}