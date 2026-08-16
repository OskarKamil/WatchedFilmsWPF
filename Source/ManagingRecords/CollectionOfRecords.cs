using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.ManagingDatagrid;
using WatchedFilmsTracker.Source.ManagingRecords;
using WatchedFilmsTracker.Source.Views;
using static WatchedFilmsTracker.Source.ManagingRecords.CellDataType;

namespace WatchedFilmsTracker.Source.ManagingFilmsFile
{
    /// <summary>
    /// Represents a collection of record models bound to a DataGrid.
    /// This class manages creation, insertion and manipulation of records
    /// and coordinates updates with the associated <see cref="WorkingTextFile"/> and <see cref="DataGridManager"/>.
    /// </summary>
    public class CollectionOfRecords
    {
        /// <summary>
        /// The observable collection of records used as the ItemsSource for the DataGrid.
        /// </summary>
        public ObservableCollection<RecordModel> ObservableCollectionOfRecords { get; }

        /// <summary>
        /// A cached list of DataGridTextColumn instances. Used for operations that need column metadata.
        /// </summary>
        public List<DataGridTextColumn> Columns = new();

        /// <summary>
        /// Manager responsible for DataGrid operations (adding/removing/renaming columns, etc.).
        /// Internal to allow coordination within the assembly while hiding it from external consumers.
        /// </summary>
        internal DataGridManager DataGridManager { get; }

        private WorkingTextFile workingTextFile;
        private readonly HashSet<RecordModel> subscribedRecords = new();
        private int changeDeferralDepth;
        private bool hasDeferredChange;

        public CollectionOfRecords(WorkingTextFile workingTextFile)
        {
            this.workingTextFile = workingTextFile;

            ObservableCollectionOfRecords = new ObservableCollection<RecordModel>();
            ObservableCollectionOfRecords.CollectionChanged += Records_CollectionChanged;
            DataGridManager = new DataGridManager(workingTextFile.DataGrid, ObservableCollectionOfRecords);
            DataGridManager.Changed += DataGridManager_Changed;
        }

        /// <summary>
        /// Raised when a record is added, removed or reordered, when a cell value changes,
        /// or when the persisted column structure changes.
        /// </summary>
        public event EventHandler? Changed;

        /// <summary>
        /// Creates an empty record and adds it to the list of records.
        /// Sets all cells of the new record as Number type. TO BE CHANGED LATER
        ///     TODO: When created, check column type and make cell type the same as column type.
        /// </summary>
        public void AddEmptyRecordToList()
        {
            RecordModel newRecord = new RecordModel(new List<Cell>());

            for (int i = 0; i < DataGridManager.DataGrid.Columns.Count; i++)
            {
                DataType newCellDataType = DataGridManager.ColumnsAndDataTypes[i].DataType;
                newRecord.AddNewCell(newCellDataType);
            }

            // Adding ID to the new record if ID column exists
            int indexOfColumnID = DataGridManager.GetIdOfColumnByHeader("#");
            if (indexOfColumnID != -1)
            {
                newRecord.Cells[indexOfColumnID].Value = (ObservableCollectionOfRecords.Count + 1).ToString();
            }

            if (SettingsManager.DefaultDateIsToday)
            {
                string formattedString = DateTime.Now.ToString("dd/MM/yyyy");
                int columnID = DataGridManager.GetIdOfColumnByHeader("Watch date");
                if (columnID != -1)
                    newRecord.Cells[columnID].Value = formattedString;
            }

            ObservableCollectionOfRecords.Add(newRecord);
            workingTextFile.DataGrid.SelectedCells.Clear();

            if (workingTextFile.DataGrid.ItemsSource == ObservableCollectionOfRecords)
            {
                workingTextFile.DataGrid.SelectedItem = newRecord;
                workingTextFile.DataGrid.ScrollIntoView(workingTextFile.DataGrid.SelectedItem);
            }

            StartEditingRecord(newRecord);
        }

        public void AddRecordFromText(string text, string delimiter)
        {
            RecordModel newRecord = new RecordModel(new List<Cell>());
            List<string> values = text.Split(delimiter).ToList();

            for (int i = 0; i < Columns.Count; i++)
            {
                newRecord.AddNewCell(values[i]);
            }

            //int indexOfColumnID = DataGridManager.GetIdOfColumnByHeader("#");
            //if (indexOfColumnID != -1)
            //    newRecord.Cells[indexOfColumnID].NumberValue = (ObservableCollectionOfRecords.Count + 1);

            ObservableCollectionOfRecords.Add(newRecord);
        }

        public void AddRecordFromStringList(List<string> list, string delimiter)
        {
            RecordModel newRecord = new RecordModel(new List<Cell>());

            for (int i = 0; i < Columns.Count; i++)
            {
                newRecord.AddNewCell(list[i]);
            }

            //int indexOfColumnID = DataGridManager.GetIdOfColumnByHeader("#");
            //if (indexOfColumnID != -1)
            //    newRecord.Cells[indexOfColumnID].NumberValue = (ObservableCollectionOfRecords.Count + 1);

            ObservableCollectionOfRecords.Add(newRecord);
        }

        public DataGridTextColumn CreateColumnWithIds()
        {
            BeginChangeDeferral();
            try
            {
                var newColumnInformation = CreateNewColumnAtIndex(0, "#");
                var newColumn = newColumnInformation.DataGridTextColumn;
                newColumn.DisplayIndex = 0;
                newColumn.IsReadOnly = true;
                newColumn.CanUserReorder = false;

                newColumn.Binding = new Binding($"Cells[{0}].Value");
                newColumn.SortMemberPath = ($"Cells[{0}].ComparableValue");

                for (int i = 0; i < ObservableCollectionOfRecords.Count; i++)
                {
                    ObservableCollectionOfRecords[i].Cells[0].Value = (i + 1).ToString();
                    ObservableCollectionOfRecords[i].Cells[0].DataType = DataType.Number;
                }
                newColumnInformation.DataType = DataType.Number;
                return newColumn;
            }
            finally
            {
                EndChangeDeferral();
            }
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
            BeginChangeDeferral();
            try
            {
                // todo datagrid manager should have columnsdatatypes, and each time column is inserted in the middle, it will update cells
                var column = DataGridManager.AddColumn(columnHeader);
                foreach (var RecordModel in ObservableCollectionOfRecords)
                {
                    RecordModel.AddNewCell(DataType.String);
                }

                int indexOfNewCell = DataGridManager.DataGrid.Columns.Count - 1;

                column.Binding = new Binding($"Cells[{indexOfNewCell}].Value");

                return column;
            }
            finally
            {
                EndChangeDeferral();
            }
        }

        public ColumnInformation CreateNewColumnAtIndex(int index, string columnHeader)
        {
            BeginChangeDeferral();
            try
            {
                ColumnInformation column = DataGridManager.AddColumnAtIndex(index, columnHeader);
                foreach (var RecordModel in ObservableCollectionOfRecords)
                {
                    RecordModel.InsertNewCellAt(index);
                }
                column.DataGridTextColumn.Binding = new Binding($"Cells[{index}].Value");

                ShiftBindingAfterInsertion(0);
                return column;
            }
            finally
            {
                EndChangeDeferral();
            }
        }

        public void DeleteAllRecords()
        {
            if (ObservableCollectionOfRecords.Count > 0)
                ObservableCollectionOfRecords.Clear();
        }

        public void DeleteColumnAt(int columndID)
        {
            BeginChangeDeferral();
            try
            {
                foreach (RecordModel recordModel in ObservableCollectionOfRecords)
                {
                    recordModel.Cells.RemoveAt(columndID);
                }

                DataGridManager.RemoveColumnAt(columndID);
                for (int i = columndID; i < DataGridManager.DataGrid.Columns.Count; i++)
                {
                    // Cast the column to DataGridBoundColumn or DataGridTextColumn
                    if (DataGridManager.DataGrid.Columns[i] is DataGridBoundColumn boundColumn)
                    {
                        // Update the binding to refer to the correct cell index after a column is removed
                        boundColumn.Binding = new Binding($"Cells[{i}].Value");
                    }
                }
            }
            finally
            {
                EndChangeDeferral();
            }
        }

        public void DeleteRecordFromList(RecordModel selected)
        {
            if (selected == null) { return; }

            int selectedIndex = ObservableCollectionOfRecords.IndexOf(selected);

            if (ObservableCollectionOfRecords.Count == 0 || selectedIndex < 0) return;

            BeginChangeDeferral();
            try
            {
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
            }
            finally
            {
                EndChangeDeferral();
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
        }

        public void IdentifyColumnForDeletion()
        {
            int columnID;
            var selectedCells = DataGridManager.DataGrid.SelectedCells;

            if (selectedCells.Count > 0)
            {
                var firstSelectedColumn = selectedCells.Select(sc => sc.Column).FirstOrDefault();
                columnID = DataGridManager.DataGrid.Columns.IndexOf(firstSelectedColumn);
            }
            else
            {
                Debug.WriteLine("no selected cells");
                return;
            }

            DeleteColumnAt(columnID);
        }

        public void PopulateListWithData(List<List<string>> list, string delimiter)
        {
            foreach (var record in list)
            {
                AddRecordFromStringList(record, delimiter);
            }
        }

        internal void ReplaceContents(List<DataGridTextColumn> columns, List<List<string>> records)
        {
            BeginChangeDeferral();
            try
            {
                ObservableCollectionOfRecords.Clear();
                DataGridManager.DataGrid.Columns.Clear();
                DataGridManager.ColumnsAndDataTypes.Clear();

                Columns = columns;
                DataGridManager.BuildColumnsFromList(Columns);
                PopulateListWithData(records, "\t");
                CreateColumnWithIds();
            }
            finally
            {
                EndChangeDeferral();
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
                    DataGridManager.RenameColumnAt(columnID, RenameColumnDialog.NewColumnName);
                }
            }
            else
            {
                Debug.WriteLine("no selected cells");
                return;
            }
        }

        private void AdjustColumnsRepresentation(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            return;
        }

        private void OnChanged()
        {
            if (changeDeferralDepth > 0)
            {
                hasDeferredChange = true;
                return;
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void BeginChangeDeferral()
        {
            changeDeferralDepth++;
        }

        private void EndChangeDeferral()
        {
            changeDeferralDepth--;
            if (changeDeferralDepth == 0 && hasDeferredChange)
            {
                hasDeferredChange = false;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Record_CellValueChanged(object? sender, EventArgs e)
        {
            OnChanged();
        }

        private void DataGridManager_Changed(object? sender, EventArgs e)
        {
            OnChanged();
        }

        private void Records_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SynchronizeRecordSubscriptions();
            OnChanged();
        }

        private void SynchronizeRecordSubscriptions()
        {
            HashSet<RecordModel> currentRecords = ObservableCollectionOfRecords.ToHashSet();

            foreach (RecordModel removedRecord in subscribedRecords.Except(currentRecords).ToList())
            {
                removedRecord.CellValueChanged -= Record_CellValueChanged;
                subscribedRecords.Remove(removedRecord);
            }

            foreach (RecordModel addedRecord in currentRecords.Except(subscribedRecords))
            {
                addedRecord.CellValueChanged += Record_CellValueChanged;
                subscribedRecords.Add(addedRecord);
            }
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
