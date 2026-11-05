using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WatchedFilmsTracker.Source.Buttons;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.GUIimprovements;
using WatchedFilmsTracker.Source.Managers;
using WatchedFilmsTracker.Source.Models;
using WatchedFilmsTracker.Source.Persistence;
using WatchedFilmsTracker.Source.Persistence.Json;
using WatchedFilmsTracker.Source.Views;
using static WatchedFilmsTracker.Source.Models.CommonCollections;

namespace WatchedFilmsTracker.Source.ManagingFilmsFile
{
    /// <summary>
    /// Owns an open collection's editing lifecycle and change notifications.
    /// Persistence belongs to CollectionFileStore; SQL execution and refresh state
    /// belong to the statistics service and session.
    /// </summary>
    public class CollectionFile
    {
        public CollectionOfRecords CollectionOfRecords { get; set; }

        public CommonCollectionType CommonCollectionType
        {
            get => _commonCollectionType;
            set
            {
                if (_commonCollectionType != value)
                {
                    _commonCollectionType = value;
                    Metadata.CollectionType = value.Name;
                    CommonCollectionTypeChanged?.Invoke(this, new CommonCollectionTypeChangedEventArgs(_commonCollectionType));
                }
            }
        }

        public DataGrid DataGrid { get; set; }

        public Action<object, RoutedEventArgs> DeleteRecordAction { get; set; }

        public Grid Grid { get; set; }

        public Metadata Metadata { get; private set; } = new();

        public string CompanionFilePath => string.IsNullOrWhiteSpace(Filepath)
            ? string.Empty
            : MetadataStore.GetSidecarPath(Filepath);

        public bool CompanionFileExists => File.Exists(CompanionFilePath);

        /// <summary>The delimiter used on subsequent saves; changing it never reparses live records.</summary>
        public string Delimiter
        {
            get => DelimiterOptions.IsSupported(Metadata.Delimiter) ? Metadata.Delimiter! : "\t";
            set
            {
                if (!DelimiterOptions.IsSupported(value))
                    throw new ArgumentException("Choose a supported delimiter.", nameof(value));
                if (Delimiter == value)
                    return;

                Metadata.Delimiter = value;
                NotifyMetadataChanged();
            }
        }

        /// <summary>
        /// Indicates whether the content has changed since the last load, save or revert.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set
            {
                if (_hasUnsavedChanges != value)
                {
                    string filename = Path.GetFileName(Filepath);
                    Debug.WriteLine("Unsaved changes set to: " + value + " for file: " + filename);
                    _hasUnsavedChanges = value;
                    UnsavedChangesChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string Filepath = string.Empty;

        private CommonCollectionType _commonCollectionType;

        private readonly CollectionFileStore fileStore = new();

        private bool isLoading;

        private bool _hasUnsavedChanges;

        private CollectionOfRecords? _subscribedCollectionOfRecords;

        public CollectionFile(CommonCollectionType commonCollection)
        {
            CommonCollectionType = commonCollection;
            DataGrid = CreateGridInTheUI();
            CollectionOfRecords = new CollectionOfRecords(this);

            CollectionOfRecords.CreateColumnWithIds();

            DataGrid.ItemsSource = GetObservableCollectionOfRecords();
            CollectionOfRecords.CreateDefaultColumnsForCommonCollectionType();
            AfterFileHasBeenLoaded();
        }

        public CollectionFile(string filepath)
        {
            CommonCollectionType = CommonCollections.GetCommonCollectionByName(CollectionType.Other);
            DataGrid = CreateGridInTheUI();
            Filepath = filepath;
            CollectionOfRecords = new CollectionOfRecords(this);

            ReadTextFile();

            DataGrid.ItemsSource = GetObservableCollectionOfRecords();
            AfterFileHasBeenLoaded();
        }

        /// <summary>
        /// Raised for every content mutation, including subsequent mutations while the file is already dirty.
        /// </summary>
        public event EventHandler? ContentChanged;

        public event EventHandler? MetadataChanged;

        public event EventHandler? MetadataReplaced;

        public event EventHandler? FileSaved;

        public event EventHandler<CommonCollectionTypeChangedEventArgs>? CommonCollectionTypeChanged;

        public event EventHandler? FileClosing;

        /// <summary>
        /// Raised only when <see cref="HasUnsavedChanges"/> changes between dirty and clean.
        /// </summary>
        public event EventHandler? UnsavedChangesChanged;

        public void AfterFileHasBeenLoaded()
        {
            SubscribeToCollectionChanges();
            HasUnsavedChanges = false;

            DataGrid.AlternatingRowBackground = new SolidColorBrush(SystemAccentColour.GetBrightAccentColourRGB());

            SettingsManager.LastPath = Filepath;

            if (SettingsManager.ScrollLastPosition)
                ScrollToBottomOfList();

            DataGrid.SelectedCellsChanged -= SelectedCellsChanged;
            DataGrid.SelectedCellsChanged += SelectedCellsChanged;
        }

        private void SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e) =>
            ButtonStateManager.UpdateSelectedCells(this);

        private void MarkAsModified()
        {
            HasUnsavedChanges = true;
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        public DataGrid CreateGridInTheUI()
        {
            var view = CollectionGridFactory.Create();
            Grid = view.Container;
            return view.RecordsGrid;
        }

        public bool DoesExistInLocalMyDataFolder() =>
            !string.IsNullOrEmpty(Filepath) &&
            string.Equals(Path.GetFileName(Path.GetDirectoryName(Filepath)), "MyData", StringComparison.OrdinalIgnoreCase);

        public bool DoesExistOnDisk() => File.Exists(Filepath);

        public DataGridManager GetDataGridManager()
        {
            return CollectionOfRecords.DataGridManager;
        }

        public ObservableCollection<RecordModel> GetObservableCollectionOfRecords()
        {
            return CollectionOfRecords.ObservableCollectionOfRecords;
        }

        public IList<DataGridCellInfo> GetSelectedCells()
        {
            return DataGrid.SelectedCells;
        }

        public bool HasAtLeastOneRecord()
        {
            return GetObservableCollectionOfRecords().Count > 0;
        }

        public bool HasSelectedCells()
        {
            return DataGrid.SelectedCells.Count > 0;
        }

        public void OpenFilepath(string? newFilePath)
        {
            if (string.IsNullOrWhiteSpace(newFilePath) || !File.Exists(newFilePath))
                return;

            Filepath = newFilePath;
            ReadTextFile();
            AfterFileHasBeenLoaded();
        }

        public void OpenFilepathButSaveChangesFirst(string? newFilePath)
        {
            if (HasUnsavedChanges && !Save())
                return;
            OpenFilepath(newFilePath);
        }

        public void ReadTextFile()
        {
            isLoading = true;
            try
            {
                var document = fileStore.Load(Filepath);
                Metadata = document.Metadata;
                ApplyMetadataCollectionType();
                CollectionOfRecords.ReplaceContents(
                    document.Data.Columns.Select(header => new DataGridTextColumn { Header = header }).ToList(),
                    document.Data.Rows.Select(row => row.ToList()).ToList());
            }
            finally
            {
                isLoading = false;
            }

            MetadataReplaced?.Invoke(this, EventArgs.Empty);
        }

        /// <returns>
        /// Returns true is file has been saved. Returns false is file has not been saved.
        /// </returns>
        public bool Save()
        {
            string filePath = Filepath;
            bool saved = false;

            if (string.IsNullOrEmpty(filePath) || !Path.Exists(Path.GetDirectoryName(filePath)))
            {
                saved = SaveAs();
                return saved;
            }

            WriteTextFile();
            HasUnsavedChanges = false;
            FileSaved?.Invoke(this, EventArgs.Empty);

            return true;
        }

        /// <returns>
        /// Returns true if user pressed Save. Returns false if user pressed Cancel or closed the window.
        /// </returns>
        public bool SaveAs()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save file as";
            saveFileDialog.Filter = "Text files (*.txt), (*.csv)|*.txt;*.csv";

            if (!string.IsNullOrEmpty(Filepath))
            {
                string parentDirectory = Directory.GetParent(Filepath)?.FullName;
                if (!string.IsNullOrEmpty(parentDirectory))
                {
                    saveFileDialog.InitialDirectory = parentDirectory;
                }
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                WriteTextFile(saveFileDialog.FileName);
                HasUnsavedChanges = false;
                FileSaved?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        public bool RevertChanges()
        {
            if (!HasUnsavedChanges)
                return true;

            if (string.IsNullOrEmpty(Filepath))
            {
                List<DataGridTextColumn> defaultColumns = CommonCollectionType.DefaultColumnHeaders
                    .Select(header => new DataGridTextColumn { Header = header })
                    .ToList();

                isLoading = true;
                try
                {
                    Metadata = new Metadata { CollectionType = CommonCollectionType.Name };
                    CollectionOfRecords.ReplaceContents(defaultColumns, new List<List<string>>());
                }
                finally
                {
                    isLoading = false;
                }
                MetadataReplaced?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                if (!File.Exists(Filepath))
                    return false;

                ReadTextFile();
            }

            HasUnsavedChanges = false;
            return true;
        }

        public void SaveFileAtLocation(string filepath)
        {
            WriteTextFile(filepath);
            HasUnsavedChanges = false;
            FileSaved?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyMetadataChanged()
        {
            HasUnsavedChanges = true;
            MetadataChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Applies companion storage immediately without saving or reloading the live records.
        /// The caller is responsible for confirming deletion before setting deleteExisting.
        /// </summary>
        public void SetCompanionStorage(bool enabled, bool deleteExisting = false)
        {
            if (enabled && (string.IsNullOrWhiteSpace(Filepath) || !File.Exists(Filepath)))
                throw new InvalidOperationException("Save the collection before creating its JSON companion file.");

            bool originalStorage = Metadata.StoreInJson;
            string originalPath = Metadata.SidecarPath;
            string originalError = Metadata.LoadError;
            try
            {
                if (enabled)
                {
                    Metadata.StoreInJson = true;
                    MetadataStore.Save(Filepath, Metadata);
                }
                else
                {
                    if (deleteExisting && CompanionFileExists)
                        File.Delete(CompanionFilePath);

                    Metadata.StoreInJson = false;
                    Metadata.SidecarPath = CompanionFilePath;
                    Metadata.LoadError = string.Empty;
                }
            }
            catch
            {
                Metadata.StoreInJson = originalStorage;
                Metadata.SidecarPath = originalPath;
                Metadata.LoadError = originalError;
                throw;
            }

            // This operation is already persisted. Preserve any existing dirty record state.
            MetadataChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Captures the live records, including unsaved edits, for persistence or SQL queries.</summary>
        public CollectionDataSnapshot CreateStatisticsSnapshot() => CollectionOfRecords.CreateSnapshot();

        public void ScrollToBottomOfList()
        {
            if (GetObservableCollectionOfRecords().Count > 0)
                DataGrid.ScrollIntoView(GetObservableCollectionOfRecords().ElementAt(GetObservableCollectionOfRecords().Count - 1));
        }

        public void setUpFilmsDataGrid(DataGrid dataGrid)
        {
            DataGrid = dataGrid;
        }

        public SaveChangesDialog.CustomDialogResult ShowSaveChangesDialog()
        {
            SaveChangesDialog dialog = new SaveChangesDialog();
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();

            return dialog.Result;
        }

        ///  <summary>
        /// If file has unsaved changes, dialog asking to save changes will display.
        /// </summary>
        /// <returns>
        /// Returns whether file can be closed.
        /// </returns>
        public bool TryToCloseFile()
        {
            if (HasUnsavedChanges)
            {
                // New file not on disk or autosave off
                // Ask to save because file not on disk, could be a draft so ask
                // Autosave off so ask
                if (string.IsNullOrEmpty(Filepath) || SettingsManager.AutoSave == false)
                {
                    SaveChangesDialog.CustomDialogResult result = ShowSaveChangesDialog();
                    if (result == SaveChangesDialog.CustomDialogResult.Save)
                    {
                        bool canClose = Save();
                        if (canClose)
                        {
                            RaiseOnClosingFile();
                        }
                        return canClose;
                    }
                    else if (result == SaveChangesDialog.CustomDialogResult.NotSave)
                    {
                        RaiseOnClosingFile();
                        return true;
                    }
                    else if (result == SaveChangesDialog.CustomDialogResult.Cancel)
                    {
                        return false;
                    }
                    return false;
                }

                // File existing on disk
                else if (SettingsManager.AutoSave)
                {
                    bool canClose = Save();
                    if (canClose)
                    {
                        RaiseOnClosingFile();
                    }
                    return canClose;
                }
            }
            RaiseOnClosingFile();
            return true;
        }

        public void WriteTextFile()
        {
            fileStore.Save(Filepath, CollectionOfRecords.CreateSnapshot(), Metadata);
        }

        public void WriteTextFile(string newFilepath)
        {
            fileStore.Save(newFilepath, CollectionOfRecords.CreateSnapshot(), Metadata);
            Filepath = newFilepath;
        }

        private void ApplyMetadataCollectionType()
        {
            // Unknown type names are valid collection identities, with generic columns and icon.
            CommonCollectionType = CommonCollections.Resolve(Metadata.CollectionType);
        }

        private void CollectionOfRecords_Changed(object? sender, EventArgs e)
        {
            if (!isLoading)
                MarkAsModified();
        }

        private void SubscribeToCollectionChanges()
        {
            if (_subscribedCollectionOfRecords != null)
                _subscribedCollectionOfRecords.Changed -= CollectionOfRecords_Changed;

            _subscribedCollectionOfRecords = CollectionOfRecords;
            _subscribedCollectionOfRecords.Changed += CollectionOfRecords_Changed;
        }

        private void RaiseOnClosingFile()
        {
            Debug.WriteLine("closing file event raised");
            FileClosing?.Invoke(this, EventArgs.Empty);
        }

        private void SystemAccentColour_AccentColorChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("Accent colour changed");
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Ensure UI updates happen on the UI thread

                DataGrid.AlternatingRowBackground = new SolidColorBrush(SystemAccentColour.GetBrightAccentColourRGB());
            });
        }

        public class CommonCollectionTypeChangedEventArgs : EventArgs
        {
            public CommonCollectionType NewCommonCollectionType { get; }

            public CommonCollectionTypeChangedEventArgs(CommonCollectionType newCommonCollectionType)
            {
                NewCommonCollectionType = newCommonCollectionType;
            }
        }
    }
}
