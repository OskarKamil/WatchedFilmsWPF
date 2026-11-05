using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WatchedFilmsTracker.Source;
using WatchedFilmsTracker.Source.BackgroundServices;
using WatchedFilmsTracker.Source.Buttons;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.ManagingDatagrid;
using WatchedFilmsTracker.Source.ManagingFilmsFile;
using WatchedFilmsTracker.Source.ManagingRecords;
using WatchedFilmsTracker.Source.Models;
using WatchedFilmsTracker.Source.Views;
using static WatchedFilmsTracker.Source.BackgroundServices.CheckForUpdateService;

namespace WatchedFilmsTracker
{
    public partial class MainWindow : Window
    {
        private SearchManager searchManager;
        private MainWindowViewModel viewModel;
        private bool isApplyingSettings = true;
        private bool isUpdatingFileInformation;
        private bool isCheckingForExit;

        public MainWindow()
        {
            //General GUI adjustments
            InitializeComponent(); // must be first line always
            viewModel = new MainWindowViewModel();
            this.DataContext = viewModel;

            Closing += MainWindow_Closing; // override closing window

            //BUTTON MANAGER
            ButtonManager.AlwaysActiveButtons.Add(buttonOpenFile);
            ButtonManager.AlwaysActiveButtons.Add(buttonAbout);
            ButtonManager.AlwaysActiveButtons.Add(ButtonNewFile);
            ButtonManager.AlwaysActiveButtons.Add(buttonOpenLocally);
            ButtonManager.AlwaysActiveButtons.Add(buttonOpenLocalFolder);

            ButtonManager.UnsavedChangeButtons.Add(buttonSave);
            ButtonManager.UnsavedChangeButtons.Add(buttonRevertChanges);

            ButtonManager.OpenedFileButtons.Add(buttonAddRecord);
            ButtonManager.OpenedFileButtons.Add(buttonAddColumn);
            ButtonManager.OpenedFileButtons.Add(buttonSaveAs);
            ButtonManager.OpenedFileButtons.Add(ButtonCurrentFileCollectionType);

            ButtonManager.AtLeastOneRecordButtons.Add(buttonSelectLast);
            ButtonManager.AtLeastOneRecordButtons.Add(buttonClearAll);

            ButtonManager.SelectedCellsButtons.Add(buttonDeleteFilmRecord);
            ButtonManager.SelectedCellsButtons.Add(buttoRemoveColumn);
            ButtonManager.SelectedCellsButtons.Add(buttoRenameColumn);

            ButtonManager.FileExistsOnDiskButtons.Add(buttonOpenContainingFolder);

            ButtonManager.FileIsNotInLocalMyDataDirectoryButtons.Add(buttonSaveLocally);

            NewFileActionPrepareContextMenu();
            CurrentlyOpenedFilePrepareContextMenu();
            DelimiterComboBox.ItemsSource = DelimiterOptions.All;

            //SETTINGS
            SettingsManager.LoadDefaultSettings();
            SettingsManager.LoadSettingsFromConfigFile();
            ApplyUserSettingsToTheProgram(); // window size, position, last path, other settings

            //CHECK UPDATE ON STARTUP
            if (SettingsManager.CheckUpdateOnStartup)
                ManualCheckForUpdate(CheckUpdatesButton, null);

            //LOCAL FILES SERVICE
            // localFilmsFilesService = new LocalFilmsFilesService(collectionFile);
            // LocalFilmsFilesService.CreateMyDataFolderIfNotExist();

            //TABCONTROL
            OpenedFilesManager.TabControl = TabControlMainWindow;
            TabControlMainWindow.ItemsSource = OpenedFilesManager.TabsOfOpenedFiles;

            TabControlMainWindow.SelectionChanged += (s, e) =>
            {
                if (!ReferenceEquals(e.Source, TabControlMainWindow))
                    return;

                UpdateCurrentCollectionUi();
            };

            OpenedFilesManager.NewFileLoaded += (sender, e) =>
            {
                e.NewWorkingTextFile.ContentChanged += WorkingTextFile_ContentChanged;
                e.NewWorkingTextFile.UnsavedChangesChanged += WorkingTextFile_UnsavedChangesChanged;
                e.NewWorkingTextFile.CommonCollectionTypeChanged += UpdateCommonCollectionElements;
                e.NewWorkingTextFile.MetadataChanged += WorkingTextFile_MetadataChanged;
                e.NewWorkingTextFile.MetadataReplaced += WorkingTextFile_MetadataChanged;
                e.NewWorkingTextFile.FileSaved += WorkingTextFile_FileSaved;
            };

            RestorePreviousSession();
            UpdateCurrentCollectionUi();

            //SNAPSHOT SERVICE
            // FileChangesSnapshotService.CreateSnapshotFolderIfNotExist();
            // FileChangesSnapshotService.FileManager = collectionFile;
            //  FileChangesSnapshotService.SubscribeToSaveCompletedEvent(this);

            //SEARCH MANAGER
            //    searchManager = new SearchManager(collectionFile, searchTextBox, dataGridMainWindow);
        }

        public event EventHandler FileOpened;

        /// <summary>
        /// This method is used in RadioButton_Checked to find what ColumnInfo nested object the control belongs to.
        /// </summary>
        /// <typeparam name="T">Searched dependency panel</typeparam>
        /// <param name="current">type of control</param>
        /// <returns></returns>
        public static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T)
                    return (T)current;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// Selects each column's RadioButton to the corresponding DataType of the ColumnInformation.
        /// </summary>
        public void ApplyColumnsDataTypesToRadioButtons()
        {
            if (GetCurrentlyOpenedTabWorkingTextFile() == null)
            {
                return;
            }
            var manager = GetCurrentlyOpenedTabWorkingTextFile().GetDataGridManager();
            foreach (var column in manager.ColumnsAndDataTypes)
            {
                column.SelectedDataType = column.DataType;
            }
        }

        public void ButtonDeleteRecord_Click(object sender, RoutedEventArgs e) // RemoveRecord, DeleteRecord
        {
            CollectionFile? collectionFile = GetCurrentlyOpenedTabWorkingTextFile();
            if (collectionFile?.HasSelectedCells() == true &&
                collectionFile.DataGrid.SelectedCells[0].Item is RecordModel selected)
            {
                collectionFile.CollectionOfRecords.DeleteRecordFromList(selected);
            }
        }

        public CollectionFile CurrentWorkingFile()
        {
            return OpenedFilesManager.CurrentlyOpenedWorkingFile();
        }

        public CollectionFile GetCurrentlyOpenedTabWorkingTextFile() => OpenedFilesManager.CurrentlyOpenedWorkingFile();

        public void UpdateDataContextForCurrentlyOpenedTabWorkingTextFile()
        {
            CollectionFile? currentlyOpened = OpenedFilesManager.CurrentlyOpenedWorkingFile();
            ItemsColumnsDataTypes.DataContext = currentlyOpened?.CollectionOfRecords.DataGridManager;
            CollectionStatisticsPanel.ShowCollection(currentlyOpened);
        }

        private void UpdateCurrentCollectionUi()
        {
            UpdateDataContextForCurrentlyOpenedTabWorkingTextFile();
            UpdateButtons();
            UpdateStageTitle();
            UpdateFileInformation();
            UpdateCommonCollectionElements(null, null);
        }

        public void UpdateStageTitle()
        {
            CollectionFile? collectionFile = GetCurrentlyOpenedTabWorkingTextFile();
            Title = collectionFile == null
                ? ProgramInformation.PROGRAM_NAME
                : $"{(collectionFile.HasUnsavedChanges ? "*" : string.Empty)}{(string.IsNullOrEmpty(collectionFile.Filepath) ? "New File" : collectionFile.Filepath)} - {ProgramInformation.PROGRAM_NAME}";
        }

        public Task UpdateStatistics() => CollectionStatisticsPanel.RefreshAsync();

        private void AboutButton(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("About dialog opened");
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this; // Set the owner window to enable modal behavior
            aboutWindow.ShowDialog();
        }

        private void ApplyChangesClick(object sender, RoutedEventArgs e)
        {
            if (GetCurrentlyOpenedTabWorkingTextFile() == null)
            {
                return;
            }
            var manager = GetCurrentlyOpenedTabWorkingTextFile().GetDataGridManager();
            foreach (var column in manager.ColumnsAndDataTypes)
            {
                manager.ChangeColumnDataType(column, column.SelectedDataType);
            }
        }

        private void ApplyUserSettingsToTheProgram()
        {
            autosaveBox.IsChecked = SettingsManager.AutoSave;
            defaultDateBox.IsChecked = SettingsManager.DefaultDateIsToday;
            checkboxCheckForUpdatesStartup.IsChecked = SettingsManager.CheckUpdateOnStartup;
            checkboxScrollLastPosition.IsChecked = SettingsManager.ScrollLastPosition;
            checkboxReopenLastFiles.IsChecked = SettingsManager.RestorePreviousSession;
            JsonForNewFilesYesRadioButton.IsChecked = SettingsManager.JsonForNewFiles == NewFileJsonPreference.Yes;
            JsonForNewFilesNoRadioButton.IsChecked = SettingsManager.JsonForNewFiles == NewFileJsonPreference.No;
            JsonForNewFilesAskRadioButton.IsChecked = SettingsManager.JsonForNewFiles == NewFileJsonPreference.AskEachTime;
            this.Left = SettingsManager.WindowLeft;
            this.Top = SettingsManager.WindowTop;
            this.Width = SettingsManager.WindowWidth;
            this.Height = SettingsManager.WindowHeight;
            FileInformationExpander.IsExpanded = SettingsManager.FileInformationExpanded;
            SettingsExpander.IsExpanded = SettingsManager.SettingsExpanded;
            TableStructureExpander.IsExpanded = SettingsManager.TableStructureExpanded;
            CollectionStatisticsExpander.IsExpanded = SettingsManager.CollectionStatisticsExpanded;
            isApplyingSettings = false;
        }

        private void SidebarPanelExpansionChanged(object sender, RoutedEventArgs e)
        {
            if (isApplyingSettings || !ReferenceEquals(sender, e.Source))
                return;

            SaveSidebarExpansionSettings();
        }

        private void SaveSidebarExpansionSettings()
        {
            SettingsManager.FileInformationExpanded = FileInformationExpander.IsExpanded;
            SettingsManager.SettingsExpanded = SettingsExpander.IsExpanded;
            SettingsManager.TableStructureExpanded = TableStructureExpander.IsExpanded;
            SettingsManager.CollectionStatisticsExpanded = CollectionStatisticsExpander.IsExpanded;
        }

        private void DelimiterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isUpdatingFileInformation && DelimiterComboBox.SelectedValue is string delimiter &&
                GetCurrentlyOpenedTabWorkingTextFile() is CollectionFile collectionFile)
            {
                collectionFile.Delimiter = delimiter;
            }
        }

        private void UseJsonCompanionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CollectionFile? collectionFile = GetCurrentlyOpenedTabWorkingTextFile();
            if (collectionFile == null)
                return;

            try
            {
                bool enable = UseJsonCompanionCheckBox.IsChecked == true;
                if (enable)
                {
                    collectionFile.SetCompanionStorage(enabled: true);
                }
                else
                {
                    JsonCompanionDialog.Choice answer = JsonCompanionDialog.Choice.Secondary;
                    if (collectionFile.CompanionFileExists)
                    {
                        var dialog = new JsonCompanionDialog(creatingNewCollection: false, collectionFile.CompanionFilePath)
                        {
                            Owner = this
                        };
                        dialog.ShowDialog();
                        answer = dialog.Result;
                    }

                    if (answer != JsonCompanionDialog.Choice.Cancel)
                        collectionFile.SetCompanionStorage(enabled: false, deleteExisting: answer == JsonCompanionDialog.Choice.Primary);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                MessageBox.Show(this, $"Could not update the JSON companion file: {exception.Message}",
                    "JSON companion file", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                // Refresh after cancellation or a failed write as well as successful changes.
                UseJsonCompanionCheckBox.GetBindingExpression(CheckBox.IsCheckedProperty)?.UpdateTarget();
                UpdateFileInformation();
            }
        }


        private void ButtonAddColumn_Click(object sender, RoutedEventArgs e)
        {
            OpenedFilesManager.CurrentlyOpenedWorkingFile()?.CollectionOfRecords.CreateNewColumn("Column");
        }

        private void ButtonAddRecord_Click(object sender, RoutedEventArgs e) // AddRecord, NewRecord
        {
            OpenedFilesManager.CurrentlyOpenedWorkingFile()?.CollectionOfRecords.AddEmptyRecordToList();
        }

        private void ButtonCurrentFileCollectionType_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            button.ContextMenu.IsOpen = true;
        }

        private void ButtonNewFile_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            button.ContextMenu.IsOpen = true;
        }

        private void ButtonOpenFileLocally_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Open file";
            openFileDialog.Filter = "Text files (*.txt), (*.csv)|*.txt;*.csv";

            string programDirectory = Environment.CurrentDirectory;
            string myDataDirectory = Path.Combine(programDirectory, "MyData");
            if (!Directory.Exists(myDataDirectory))
            {
                Directory.CreateDirectory(myDataDirectory);
            }

            openFileDialog.InitialDirectory = myDataDirectory;

            if (openFileDialog.ShowDialog() == true)
            {
                OpenedFilesManager.CreateNewWorkingFile(openFileDialog.FileName);
            }
        }

        private void CheckBoxAutoSave(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            SettingsManager.AutoSave = (bool)checkBox.IsChecked;
            checkBox.IsChecked = SettingsManager.AutoSave;
        }

        private void CheckBoxDefaultDate(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            SettingsManager.DefaultDateIsToday = (bool)checkBox.IsChecked;
            checkBox.IsChecked = SettingsManager.DefaultDateIsToday;
        }

        private void CheckBoxLastFilmPosition(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            SettingsManager.ScrollLastPosition = (bool)checkBox.IsChecked;
            checkBox.IsChecked = SettingsManager.ScrollLastPosition;
        }

        private void CheckBoxReopenFilesLastSession(object sender, RoutedEventArgs e)
        {
            SettingsManager.RestorePreviousSession = checkboxReopenLastFiles.IsChecked == true;
        }

        private void JsonForNewFilesPreference_Click(object sender, RoutedEventArgs e)
        {
            if (isApplyingSettings)
                return;

            SettingsManager.JsonForNewFiles = JsonForNewFilesYesRadioButton.IsChecked == true
                ? NewFileJsonPreference.Yes
                : JsonForNewFilesNoRadioButton.IsChecked == true
                    ? NewFileJsonPreference.No
                    : NewFileJsonPreference.AskEachTime;
        }

        private void CheckBoxUpdateStartup(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)(sender);
            SettingsManager.CheckUpdateOnStartup = (bool)checkBox.IsChecked;
        }

        private void ClearAll(object sender, RoutedEventArgs e)
        {
            GetCurrentlyOpenedTabWorkingTextFile()?.CollectionOfRecords.DeleteAllRecords();
        }

        private void CurrentlyOpenedFilePrepareContextMenu()
        {
            ContextMenu contextMenu = new ContextMenu();

            foreach (CommonCollectionType commonCollection in CommonCollections.GetAllCollectionsAlphabetically())
            {
                MenuItem menuItem = new MenuItem
                {
                    Header = commonCollection.Name,
                    Icon = commonCollection.GetIconImage(),
                    Tag = commonCollection.Name,
                    IsCheckable = true,
                };

                menuItem.Click += (sender, e) =>
                {
                    CollectionFile? collectionFile = GetCurrentlyOpenedTabWorkingTextFile();
                    if (collectionFile == null)
                        return;
                    collectionFile.CommonCollectionType = commonCollection;
                    collectionFile.NotifyMetadataChanged();
                };
                contextMenu.Items.Add(menuItem);
            }

            ButtonCurrentFileCollectionType.ContextMenu = contextMenu;
        }

        private void DEBUG_MakeAllColumnsText(object sender, RoutedEventArgs e)
        {
            var workingFile = OpenedFilesManager.CurrentlyOpenedWorkingFile();

            if (workingFile == null)
                return;

            workingFile.GetDataGridManager().ChangeDataTypeAllColumns(CellDataType.DataType.String);

            ApplyColumnsDataTypesToRadioButtons();
            Debug.WriteLine("All columns changed to text");
        }

        private void DEBUG_PrintCellsDataTypes(object sender, RoutedEventArgs e)
        {
            var workingFile = OpenedFilesManager.CurrentlyOpenedWorkingFile();

            if (workingFile == null)
                return;

            var collection = workingFile.CollectionOfRecords.ObservableCollectionOfRecords;

            Debug.WriteLine("Values and types of each cell:");
            foreach (var record in collection)
            {
                foreach (var cell in record.Cells)
                {
                    Debug.WriteLine($"Cell value: {cell.Value}, Cell data type: {cell.DataType}");
                }
            }
        }

        private void DEBUG_PrintColumnTypes(object sender, RoutedEventArgs e)
        {
            var workingFile = OpenedFilesManager.CurrentlyOpenedWorkingFile();

            if (workingFile == null)
                return;

            Debug.WriteLine("Columns and their data types:");
            foreach (var column in workingFile.CollectionOfRecords.DataGridManager.ColumnsAndDataTypes)
            {
                Debug.WriteLine(column);
            }
        }

        private void filmsGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (e.Cancel)
                return;
            if (isCheckingForExit)
            {
                e.Cancel = true;
                return;
            }

            isCheckingForExit = true;
            CollectionFile? selectedBeforeClose = GetCurrentlyOpenedTabWorkingTextFile();
            CollectionFile[] files = OpenedFilesManager.OpenedFiles.ToArray();
            try
            {
                foreach (CollectionFile collectionFile in files)
                {
                    if (!collectionFile.DataGrid.CommitEdit(DataGridEditingUnit.Cell, true) ||
                        !collectionFile.DataGrid.CommitEdit(DataGridEditingUnit.Row, true))
                    {
                        OpenedFilesManager.SelectFile(collectionFile);
                        e.Cancel = true;
                        return;
                    }

                    if (!collectionFile.HasUnsavedChanges)
                        continue;

                    OpenedFilesManager.SelectFile(collectionFile);
                    if (!collectionFile.TryPrepareForClose(useAutoSave: false, owner: this))
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                // Capture paths after Save As supplies names for saved drafts. Keep tabs
                // and declined edits intact until every close confirmation succeeds.
                SettingsManager.SessionFilePaths = files.Select(file => file.Filepath).ToArray();
                SettingsManager.ActiveSessionFilePath = !string.IsNullOrWhiteSpace(selectedBeforeClose?.Filepath)
                    ? selectedBeforeClose.Filepath
                    : SettingsManager.SessionFilePaths.LastOrDefault() ?? string.Empty;
                SettingsManager.LastPath = SettingsManager.ActiveSessionFilePath;
                SettingsManager.WindowLeft = Left;
                SettingsManager.WindowTop = Top;
                SettingsManager.WindowWidth = Width;
                SettingsManager.WindowHeight = Height;
                SaveSidebarExpansionSettings();
                SettingsManager.SaveToConfFile();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                e.Cancel = true;
                MessageBox.Show(this, $"Could not finish closing the application: {exception.Message}",
                    "Close application", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                isCheckingForExit = false;
            }
        }

        private async void ManualCheckForUpdate(object sender, RoutedEventArgs e)
        {
            // Disable the button and update the UI
            CheckUpdatesButton.IsEnabled = false;
            SettingsUpdateBlock.Text = "Checking for update...";
            ImageNewVersionSettings.Visibility = Visibility.Collapsed;
            await UpdateVersionInformationAsync();
        }

        private void NewFileActionPrepareContextMenu()
        {
            ContextMenu contextMenu = new ContextMenu();

            foreach (CommonCollectionType commonCollection in CommonCollections.GetAllCollectionsAlphabetically())
            {
                MenuItem menuItem = new MenuItem
                {
                    Header = commonCollection.Name,
                    Icon = commonCollection.GetIconImage(),
                };

                menuItem.Click += (sender, e) =>
                {
                    bool useJson = SettingsManager.JsonForNewFiles == NewFileJsonPreference.Yes;
                    if (SettingsManager.JsonForNewFiles == NewFileJsonPreference.AskEachTime)
                    {
                        var dialog = new JsonCompanionDialog(creatingNewCollection: true) { Owner = this };
                        dialog.ShowDialog();
                        if (dialog.Result == JsonCompanionDialog.Choice.Cancel)
                            return;
                        useJson = dialog.Result == JsonCompanionDialog.Choice.Primary;
                    }

                    OpenedFilesManager.CreateEmptyWorkingFile(commonCollection);
                    CollectionFile newCollection = OpenedFilesManager.OpenedFiles.Last();
                    newCollection.SetCompanionStorage(useJson);
                    if (useJson && !newCollection.HasUnsavedChanges)
                        newCollection.NotifyMetadataChanged();
                };
                contextMenu.Items.Add(menuItem);
            }

            ButtonNewFile.ContextMenu = contextMenu;
        }

        private void OpenContainingFolder(object sender, RoutedEventArgs e)
        {
            if (File.Exists(OpenedFilesManager.CurrentlyOpenedWorkingFile().Filepath))
            {
                Process.Start("explorer.exe", "/select, " + OpenedFilesManager.CurrentlyOpenedWorkingFile().Filepath);
            }
            else
            {
                Debug.WriteLine($"{OpenedFilesManager.CurrentlyOpenedWorkingFile().Filepath} cannot be found");
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Open file";
            openFileDialog.Filter = "Text files (*.txt), (*.csv)|*.txt;*.csv";

            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            // todo instead of current directory, open the same path of most recent opened file

            if (openFileDialog.ShowDialog() == true)
            {
                OpenedFilesManager.CreateNewWorkingFile(openFileDialog.FileName);
            }
        }

        private void RestorePreviousSession()
        {
            if (!SettingsManager.RestorePreviousSession)
                return;

            string[] paths = SettingsManager.SessionFilePaths.ToArray();
            string activePath = SettingsManager.ActiveSessionFilePath;
            List<string> errors = new();
            foreach (string path in paths)
            {
                if (!File.Exists(path))
                    continue;

                try
                {
                    OpenedFilesManager.CreateNewWorkingFile(path);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException)
                {
                    errors.Add($"{path}\n{exception.Message}");
                }
            }

            if (OpenedFilesManager.FindOpenFile(activePath) is CollectionFile activeFile)
                OpenedFilesManager.SelectFile(activeFile);

            if (errors.Count > 0)
                MessageBox.Show(this, "Some files from the previous session could not be reopened:\n\n" + string.Join("\n\n", errors),
                    "Restore previous session", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenLocalFolderButton_Click(object sender, RoutedEventArgs e)
        {
            LocalFilmsFilesService.OpenMyDataDirectoryFileExplorer();
        }

        /// <summary>
        /// Checks if DataContext of RadioButton is of CellDataType.DataType.
        /// Finds ColumnInformation associated with RadioButton.
        /// Sets the columnInfo.SelectedDataType to columnInfo.SelectedDataType
        /// which is RadioButton DataContext value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var clickedRadioButton = sender as RadioButton;
            if (clickedRadioButton == null) return;

            // Get the selected type from the RadioButton content
            if (clickedRadioButton.DataContext is CellDataType.DataType selectedType)
            {
                // Navigate up the visual tree to find the parent ColumnInformation
                var columnInfo = FindAncestor<StackPanel>(clickedRadioButton)?.DataContext as ColumnInformation;
                if (columnInfo != null)
                {
                    columnInfo.SelectedDataType = selectedType;
                }
            }
        }

        private void RemoveColumnButton_Click(object sender, RoutedEventArgs e)
        {
            OpenedFilesManager.CurrentlyOpenedWorkingFile()?.CollectionOfRecords.IdentifyColumnForDeletion();
        }

        private void RenameColumnButton_Click(object sender, RoutedEventArgs e)
        {
            OpenedFilesManager.CurrentlyOpenedWorkingFile()?.CollectionOfRecords.RenameColumn();
        }

        private void ResetColumnsWidthAndOrder(object sender, RoutedEventArgs e)
        {
            GetCurrentlyOpenedTabWorkingTextFile()?.CollectionOfRecords.DataGridManager.ResetToDefault();
        }

        private void RevertChangesButton_Click(object sender, RoutedEventArgs e)
        {
            CollectionFile? workingTextFile = GetCurrentlyOpenedTabWorkingTextFile();
            if (workingTextFile?.RevertChanges() == true)
                UpdateCurrentCollectionUi();
        }

        private void SaveAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (CollectionFile collectionFile in OpenedFilesManager.OpenedFiles)
            {
                if (collectionFile.HasUnsavedChanges)
                    collectionFile.Save();
            }
        }

        private void SaveAs(object sender, RoutedEventArgs e)
        {
            GetCurrentlyOpenedTabWorkingTextFile()?.SaveAs();
        }

        private void SaveButtonAction(object sender, RoutedEventArgs e)
        {
            GetCurrentlyOpenedTabWorkingTextFile()?.Save();
        }

        private void SaveLocally(object sender, RoutedEventArgs e)
        {
            if (GetCurrentlyOpenedTabWorkingTextFile() is CollectionFile collectionFile)
                new LocalFilmsFilesService(collectionFile).SaveFileInProgramDirectory();
        }

        private void searchClearButton_ClearText(object sender, RoutedEventArgs e)
        {
            searchTextBox.Clear();
            searchTextBox.Focus();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (clearSearchButton == null)
            {
                return;
            }
            if (searchTextBox.Text.Length > 0)
            {
                clearSearchButton.Visibility = Visibility.Visible;
            }
            else
            {
                clearSearchButton.Visibility = Visibility.Hidden;
            }
            searchManager?.SearchFilms();

            return;
        }

        private void SelectLastButton(object sender, RoutedEventArgs e)

        {
            GetCurrentlyOpenedTabWorkingTextFile()?.ScrollToBottomOfList();
        }


        private void UpdateButtons()
        {
            ButtonStateManager.UpdateAllButton(GetCurrentlyOpenedTabWorkingTextFile());
            UpdateSaveAllButton();
        }

        private void UpdateSaveAllButton() =>
            ButtonManager.SetButtonsState(new List<Button> { buttonSaveAll },
                OpenedFilesManager.OpenedFiles.Any(file => file.HasUnsavedChanges));

        private void UpdateCommonCollectionElements(object? sender, CollectionFile.CommonCollectionTypeChangedEventArgs? e)
        {
            CollectionFile? collectionFile = GetCurrentlyOpenedTabWorkingTextFile();
            TextBlockCurrentFileCollectionType.Text = collectionFile?.CommonCollectionType.Name ?? "Other";
            ImageCurrentFileCollectionType.Source = (collectionFile?.CommonCollectionType ?? CommonCollections.Resolve("Other")).GetIconImageSource();

            if (ButtonCurrentFileCollectionType.ContextMenu != null)
            {
                foreach (MenuItem item in ButtonCurrentFileCollectionType.ContextMenu.Items.OfType<MenuItem>())
                {
                    item.IsChecked = string.Equals(item.Tag as string,
                        collectionFile?.CommonCollectionType.Name, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        private void UpdateFileInformation()
        {
            CollectionFile? collectionFile = GetCurrentlyOpenedTabWorkingTextFile();
            isUpdatingFileInformation = true;
            try
            {
                TextBoxFilePath.Text = collectionFile?.Filepath ?? string.Empty;
                DelimiterComboBox.SelectedValue = collectionFile?.Delimiter;
                FileInformationBody.IsEnabled = collectionFile != null;
                TableStructureBody.IsEnabled = collectionFile != null;
            }
            finally
            {
                isUpdatingFileInformation = false;
            }
        }

        private void WorkingTextFile_MetadataChanged(object? sender, EventArgs e)
        {
            if (ReferenceEquals(sender, GetCurrentlyOpenedTabWorkingTextFile()))
                UpdateFileInformation();
        }

        private void WorkingTextFile_FileSaved(object? sender, EventArgs e)
        {
            if (ReferenceEquals(sender, GetCurrentlyOpenedTabWorkingTextFile()))
                UpdateCurrentCollectionUi();
        }

        private void WorkingTextFile_ContentChanged(object? sender, EventArgs e)
        {
            if (ReferenceEquals(sender, GetCurrentlyOpenedTabWorkingTextFile()))
                UpdateButtons();
        }

        private void WorkingTextFile_UnsavedChangesChanged(object? sender, EventArgs e)
        {
            UpdateSaveAllButton();
            if (!ReferenceEquals(sender, GetCurrentlyOpenedTabWorkingTextFile()))
                return;

            ButtonStateManager.UpdateUnsavedChanges(GetCurrentlyOpenedTabWorkingTextFile());
            UpdateStageTitle();
        }


        private async Task UpdateVersionInformationAsync()
        {
            UpdateStatusCheck isNewVersionAvailable;
            try
            {
                isNewVersionAvailable = await CheckForUpdateService.IsNewerVersionOnGitHubAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error has occured: {ex.Message}");
                isNewVersionAvailable = UpdateStatusCheck.ErrorChecking;
            }
            await Task.Delay(TimeSpan.FromSeconds(2)); // imitate long checking, just in case

            if (isNewVersionAvailable == UpdateStatusCheck.UpdateAvailable)
            {
                // Apply colour and display icon
                ImageNewVersion.Visibility = System.Windows.Visibility.Visible;
                RadialGradientBrush updateBrush = new RadialGradientBrush();
                updateBrush.GradientOrigin = new Point(0.5, 0.5);
                updateBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.5));
                updateBrush.GradientStops.Add(new GradientStop(Colors.Gold, 1.0));
                PanelVersion.Background = updateBrush;

                // Set hyperlink to open release page
                string uri = "https://github.com/OskarKamil/WatchedFilmsWPF/releases/tag/" + CheckForUpdateService.NewVersion;
                TextBlockNewVersion.Visibility = System.Windows.Visibility.Visible;
                Hyperlink releasesPage = new Hyperlink() { NavigateUri = new Uri(uri) };
                releasesPage.RequestNavigate += (sender, e) =>
                {
                    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                };
                releasesPage.Inlines.Add($"{CheckForUpdateService.NewVersion} is available. Click here to open download page.");

                TextBlockNewVersion.Inlines.Add(releasesPage);

                CheckUpdatesButton.IsEnabled = true;
                CheckUpdatesButton.Content = "Download page";
                SettingsUpdateBlock.Text = "Update found.";
                ImageNewVersionSettings.Visibility = Visibility.Visible;
                CheckUpdatesButton.Click -= ManualCheckForUpdate;
                CheckUpdatesButton.Click += (sender, e) =>
                {
                    Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                };
            }
            else if (isNewVersionAvailable == UpdateStatusCheck.NoUpdate)
            {
                CheckUpdatesButton.IsEnabled = true;
                ImageNewVersionSettings.Visibility = Visibility.Collapsed;
                SettingsUpdateBlock.Text = "No update found.";
            }
            else if (isNewVersionAvailable == UpdateStatusCheck.ErrorChecking)
            {
                CheckUpdatesButton.IsEnabled = true; SettingsUpdateBlock.Text = "Error checking.";
            }
            return;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.D) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                Width = 1200;
                Height = 600;
            }
        }
    }
}
