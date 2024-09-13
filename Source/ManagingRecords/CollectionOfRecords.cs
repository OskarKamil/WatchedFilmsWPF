using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.Services.Csv;
using WatchedFilmsTracker.Source.Views;

namespace WatchedFilmsTracker.Source.ManagingFilmsFile
{
    public class CollectionOfRecords
    {
        public ObservableCollection<RecordModel> ObservableCollectionOfRecords { get; set; }
        internal DataGridManager DataGridManager { get; set; }
        private List<DataGridTextColumn> columns;
        private string fileColumnHeaders;
        private CSVreader reader;
        private WorkingTextFile workingTextFile;
        private CSVwriter writer;

        public CollectionOfRecords(string filePath, WorkingTextFile filmsTextFile)
        {
            this.workingTextFile = filmsTextFile;

            if (string.IsNullOrEmpty(filePath))
                ObservableCollectionOfRecords = new ObservableCollection<RecordModel>();
            else
                ReadTextFile(filePath);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddEmptyRecordToList()
        {
            //todo new record is never added
            // maybe the grid is never connected to obserable collection
            RecordModel newRecord = new RecordModel(new List<Cell>());

            for (int i = 0; i < DataGridManager.DataGrid.Columns.Count; i++)
            {
                {
                    newRecord.Cells.Add(new Cell(string.Empty));
                }
            }

            int indexOfColumnID = DataGridManager.GetIdOfColumnByHeader("#");
            newRecord.Cells[indexOfColumnID].Value = (ObservableCollectionOfRecords.Count + 1).ToString();

            newRecord.CellChanged += workingTextFile.FilmRecord_PropertyChanged;
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

            //StartEditingNewRecord();

            workingTextFile.AnyChangeHappen();
        }

        public void CloseReader()
        {
            reader.CloseFile();
        }

        public void CreateColumnsInBlankFile()
        {
            CreateColumnsWithIds();
            foreach (string columnHeader in workingTextFile.CommonCollectionType.DefaultColumnHeaders)
            {
                CreateNewColumn(columnHeader);
            }
        }

        public DataGridTextColumn CreateNewColumn(string columnHeader)
        {
            var column = DataGridManager.AddColumn(columnHeader);
            foreach (var RecordModel in ObservableCollectionOfRecords)
            {
                RecordModel.AddNewCell();
            }

            int indexOfNewCell = DataGridManager.DataGrid.Columns.Count - 1;

            column.Binding = new Binding($"Cells[{indexOfNewCell}].Value");

            ProgramStateManager.IsUnsavedChange = true;

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
            ProgramStateManager.IsUnsavedChange = true;
        }

        public void DeleteRecordFromList(RecordModel selected)
        {
            // fix
            //if (selected == null) { return; }

            //int selectedIndex = workingTextFile.DataGrid.Items.IndexOf(selected);

            //if (observableCollectionOfRecords.Count == 0 || selected.IdInList == 0) return;
            //int idOfSelected = selected.IdInList;
            //observableCollectionOfRecords.Remove(selected);
            //RefreshFurtherIDs(idOfSelected);

            //// selecting the next record
            //workingTextFile.DataGrid.SelectedCells.Clear();
            //if (selectedIndex + 0 == workingTextFile.DataGrid.Items.Count)
            //{
            //    workingTextFile.DataGrid.SelectedIndex = selectedIndex - 1;
            //}
            //else
            //{
            //    workingTextFile.DataGrid.SelectedIndex = selectedIndex - 0;
            //}
        }

        public void ReadTextFile(string filePath)
        {
            reader = new CSVreader();
            ObservableCollectionOfRecords = reader.ReadCsvReturnObservableCollection(filePath);
            columns = reader.GetColumns();
            DataGridManager = new DataGridManager(workingTextFile.DataGrid);
            DataGridManager.BuildColumnsFromList(columns);
        }

        public void RefreshFurtherIDs(int idOfSelected)
        {
            for (int i = 0; i < ObservableCollectionOfRecords.Count; i++)
            {
                RecordModel record = ObservableCollectionOfRecords[i];
                // if (record.IdInList >= idOfSelected) record.IdInList--;
                //todo fix
            }
        }

        public void StartWriter(string newFilePath)
        {
            if (reader != null) CloseReader();
            writer = new CSVwriter(newFilePath);

            writer.SaveListIntoCSV(ObservableCollectionOfRecords.ToList(), workingTextFile.DataGrid);
            CloseWriter();
        }

        internal void CreateColumnsWithIds()
        {
            var newColumn = CreateNewColumn("#");
            newColumn.Header = "#";
            newColumn.DisplayIndex = 0;
            newColumn.IsReadOnly = true;
            newColumn.CanUserReorder = false;

            int indexOfColumnID = DataGridManager.GetIdOfColumnByHeader("#");
            for (int i = 0; i < ObservableCollectionOfRecords.Count; i++)
            {
                ObservableCollectionOfRecords[i].Cells[indexOfColumnID].Value = (i + 1).ToString();
            }
            ProgramStateManager.IsUnsavedChange = false;
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
                    ProgramStateManager.IsUnsavedChange = true;
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

        private void CloseWriter()
        {
            writer?.Close();
        }

        private void StartEditingNewRecord()
        {
            if (DataGridManager.GetNumberOfColumns() == 0)
                return;

            int columnID = DataGridManager.GetIdOfColumnByHeader("English title");

            Debug.WriteLine($"no of columns {DataGridManager.GetNumberOfColumns()}, columnId of english title {columnID}, number of items in datagrid {workingTextFile.DataGrid.Items.Count}, no of columns nin datagrid {workingTextFile.DataGrid.Columns.Count}");

            //number of items in datagrid is 0, check why

            if (columnID != -1)
                workingTextFile.DataGrid.CurrentCell = new DataGridCellInfo(workingTextFile.DataGrid.Items[workingTextFile.DataGrid.Items.Count - 1], workingTextFile.DataGrid.Columns[columnID]);

            workingTextFile.DataGrid.BeginEdit();
        }
    }
}