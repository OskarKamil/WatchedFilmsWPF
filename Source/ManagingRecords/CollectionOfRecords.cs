using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.Services.Csv;
using WatchedFilmsTracker.Source.Views;

namespace WatchedFilmsTracker.Source.ManagingFilmsFile
{
    /// <summary>
    /// Represents a collection of films. This class is responsible for storing a list of films
    /// and managing all operations on this list, such as adding, removing, or updating film records.
    /// All interactions with the film list should go through this class.
    /// </summary>
    public class CollectionOfRecords
    {
        public string FilePath
        {
            get { return filePath; }
            set { filePath = value; }
        }

        public ObservableCollection<RecordModel> ObservableCollectionOfRecords
        {
            get { return observableCollectionOfRecords; }
        }

        internal DataGridManager DataGridManager { get => dataGridManager; set => dataGridManager = value; }

        private List<DataGridTextColumn> columns;

        private DataGridManager dataGridManager;

        private string fileColumnHeaders;

        private string filePath;
        private ObservableCollection<RecordModel> observableCollectionOfRecords;
        private CSVreader reader;
        private WorkingTextFile workingTextFile;
        private CSVwriter writer;

        public CollectionOfRecords(string filePath, WorkingTextFile filmsTextFile)
        {
            this.workingTextFile = filmsTextFile;
            this.filePath = filePath;
        }

        public CollectionOfRecords(WorkingTextFile filmsTextFile)
        {
            this.workingTextFile = filmsTextFile;
            filePath = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddEmptyRecordToList()
        {
            RecordModel newRecord = new RecordModel(new List<Cell>());

            for (int i = 0; i < dataGridManager.dataGrid.Columns.Count; i++)
            {
                {
                    newRecord.Cells.Add(new Cell(string.Empty));
                }
            }

            int indexOfColumnID = dataGridManager.GetIdOfColumnByHeader("#");

            newRecord.Cells[indexOfColumnID].Value = (ObservableCollectionOfRecords.Count + 1).ToString();

            newRecord.CellChanged += workingTextFile.FilmRecord_PropertyChanged;
            observableCollectionOfRecords.Add(newRecord);
            workingTextFile.DataGrid.SelectedCells.Clear();
            if (workingTextFile.DataGrid.ItemsSource == observableCollectionOfRecords)
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

            StartEditingNewRecord();

            workingTextFile.AnyChangeHappen();
        }

        public void CloseReader()
        {
            reader.CloseFile();
        }

        public DataGridTextColumn CreateNewColumn(string columnHeader)
        {
            var column = dataGridManager.AddColumn(columnHeader);
            foreach (var RecordModel in ObservableCollectionOfRecords)
            {
                RecordModel.AddNewCell();
            }

            int indexOfNewCell = dataGridManager.dataGrid.Columns.Count - 1;

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
            var selectedCells = dataGridManager.dataGrid.SelectedCells;

            if (selectedCells.Count > 0)
            {
                var firstSelectedColumn = selectedCells.Select(sc => sc.Column).FirstOrDefault();
                columnID = dataGridManager.dataGrid.Columns.IndexOf(firstSelectedColumn);
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
            dataGridManager.dataGrid.Columns.RemoveAt(columndID);
            for (int i = columndID; i < dataGridManager.dataGrid.Columns.Count; i++)
            {
                // Cast the column to DataGridBoundColumn or DataGridTextColumn
                if (dataGridManager.dataGrid.Columns[i] is DataGridBoundColumn boundColumn)
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

        public void RefreshFurtherIDs(int idOfSelected)
        {
            for (int i = 0; i < observableCollectionOfRecords.Count; i++)
            {
                RecordModel record = observableCollectionOfRecords[i];
                // if (record.IdInList >= idOfSelected) record.IdInList--;
                //todo fix
            }
        }

        public void StartReader()
        {
            reader = new CSVreader();
            observableCollectionOfRecords = reader.ReadCsvReturnObservableCollection(filePath);
            columns = reader.GetColumns();
            DataGridManager = new DataGridManager(workingTextFile.DataGrid);
            DataGridManager.BuildColumnsFromList(columns);
        }

        public void StartWriter(string newFilePath)
        {
            if (reader != null) CloseReader();
            writer = new CSVwriter(newFilePath);

            writer.SaveListIntoCSV(observableCollectionOfRecords.ToList(), workingTextFile.DataGrid);
            CloseWriter();
        }

        internal void CreateColumnsWithIds()
        {
            var newColumn = CreateNewColumn("#");
            newColumn.Header = "#";
            newColumn.DisplayIndex = 0;
            newColumn.IsReadOnly = true;
            newColumn.CanUserReorder = false;

            int indexOfColumnID = dataGridManager.GetIdOfColumnByHeader("#");
            for (int i = 0; i < observableCollectionOfRecords.Count; i++)
            {
                observableCollectionOfRecords[i].Cells[indexOfColumnID].Value = (i + 1).ToString();
            }
            ProgramStateManager.IsUnsavedChange = false;
        }

        internal void RenameColumn()
        {
            int columnID;
            var selectedCells = dataGridManager.dataGrid.SelectedCells;

            if (selectedCells.Count > 0)
            {
                var firstSelectedColumn = selectedCells.Select(sc => sc.Column).FirstOrDefault();
                columnID = dataGridManager.dataGrid.Columns.IndexOf(firstSelectedColumn);
                var RenameColumn = new RenameColumn
                {
                    newColumnName = firstSelectedColumn.Header.ToString()
                };
                if (RenameColumn.Result == RenameColumn.CustomDialogResult.Confirm)
                {
                    firstSelectedColumn.Header = RenameColumn.newColumnName;
                    //MessageBox.Show($"New column name: {newColumnName}");
                    // Update the column name in your data or UI
                }
                else
                {
                    MessageBox.Show("Rename canceled.");
                }
            }
            else
            {
                Debug.WriteLine("no selected cells");
                return;
            }

            //todo open modal window asking for new name
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
            if (columnID != -1)
                workingTextFile.DataGrid.CurrentCell = new DataGridCellInfo(workingTextFile.DataGrid.Items[workingTextFile.DataGrid.Items.Count - 1], workingTextFile.DataGrid.Columns[columnID]);

            workingTextFile.DataGrid.BeginEdit();
        }
    }
}