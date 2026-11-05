using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using WatchedFilmsTracker.Source.ManagingFilmsFile;
using WatchedFilmsTracker.Source.Models;
using WatchedFilmsTracker.Source.Persistence.Json;
using WatchedFilmsTracker.Source.Statistics;

namespace WatchedFilmsTracker.Source.ViewModels
{
    /// <summary>
    /// Owns one collection's statistic editor, refresh policy and cached results.
    /// Queries receive an immutable snapshot of the live datasource.
    /// </summary>
    internal sealed class StatisticsSession : INotifyPropertyChanged, IDisposable
    {
        private readonly CollectionFile collectionFile;
        private readonly DispatcherTimer refreshTimer;
        private StatisticDefinition? selectedDefinition;
        private string editorName = string.Empty;
        private string editorSql = string.Empty;
        private string draftName = string.Empty;
        private string draftSql = string.Empty;
        private StatisticDefinition? selectedTemplate;
        private string status = string.Empty;
        private string definitionFingerprint;
        private bool isBusy;
        private bool isStale = true;
        private bool initialized;
        private bool disposed;
        private bool refreshPending;
        private int revision;

        public StatisticsSession(CollectionFile collectionFile)
        {
            this.collectionFile = collectionFile;
            definitionFingerprint = GetDefinitionFingerprint();
            refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            refreshTimer.Tick += RefreshTimer_Tick;
            collectionFile.ContentChanged += DatasourceChanged;
            collectionFile.MetadataChanged += MetadataChanged;
            collectionFile.MetadataReplaced += MetadataReplaced;
            collectionFile.FileSaved += FileSaved;
            SelectedDefinition = Definitions.FirstOrDefault();
            SelectedTemplate = DefaultStatisticsCatalog.Empty;
            Status = "Statistics have not been loaded. Select Refresh statistics.";
        }

        public const string SessionOnlyWarningMessage =
            "Statistic queries are not saved and will disappear when the program closes. " +
            "To keep them, tick Use JSON companion file in File information and save the collection.";

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<StatisticReportResult> Results { get; } = new();
        public ObservableCollection<StatisticDefinition> Definitions => collectionFile.Metadata.Statistics;

        public bool IsJsonEnabled
        {
            get => collectionFile.Metadata.StoreInJson;
            set
            {
                if (IsJsonEnabled == value)
                    return;

                collectionFile.Metadata.StoreInJson = value;
                collectionFile.NotifyMetadataChanged();
            }
        }

        public bool LoadOnOpen
        {
            get => collectionFile.Metadata.StatisticsSettings.LoadOnOpen;
            set
            {
                if (LoadOnOpen == value)
                    return;

                collectionFile.Metadata.StatisticsSettings.LoadOnOpen = value;
                collectionFile.NotifyMetadataChanged();
            }
        }

        public bool AutoRefresh
        {
            get => collectionFile.Metadata.StatisticsSettings.AutoRefresh;
            set
            {
                if (AutoRefresh == value)
                    return;

                collectionFile.Metadata.StatisticsSettings.AutoRefresh = value;
                collectionFile.NotifyMetadataChanged();
            }
        }

        public string MetadataPath => string.IsNullOrWhiteSpace(collectionFile.Filepath)
            ? string.Empty
            : MetadataStore.GetSidecarPath(collectionFile.Filepath);

        public string PersistenceWarning => SessionOnlyWarningMessage;

        public string MetadataStatus => !string.IsNullOrWhiteSpace(collectionFile.Metadata.LoadError)
            ? collectionFile.Metadata.LoadError
            : !IsJsonEnabled
                ? "JSON storage is off. Queries and collection settings are kept only for this session."
                : string.IsNullOrWhiteSpace(collectionFile.Filepath)
                    ? "The companion file will be created when you save the collection."
                    : "Save the collection to update its JSON companion file.";

        public StatisticDefinition? SelectedDefinition
        {
            get => selectedDefinition;
            set
            {
                if (!SetField(ref selectedDefinition, value))
                    return;

                EditorName = value?.Name ?? string.Empty;
                EditorSql = value?.Sql ?? string.Empty;
                NotifySelectionActions();
            }
        }

        public string EditorName
        {
            get => editorName;
            set => SetField(ref editorName, value);
        }

        public string EditorSql
        {
            get => editorSql;
            set => SetField(ref editorSql, value);
        }

        public StatisticDefinition? SelectedTemplate
        {
            get => selectedTemplate;
            set
            {
                if (SetField(ref selectedTemplate, value) && value != null)
                    PrepareDraft(value);
            }
        }

        public string DraftName
        {
            get => draftName;
            set => SetField(ref draftName, value);
        }

        public string DraftSql
        {
            get => draftSql;
            set => SetField(ref draftSql, value);
        }

        public bool HasSelectedDefinition => SelectedDefinition != null && Definitions.Contains(SelectedDefinition);
        public bool CanMoveUp => HasSelectedDefinition && Definitions.IndexOf(SelectedDefinition!) > 0;
        public bool CanMoveDown => HasSelectedDefinition && Definitions.IndexOf(SelectedDefinition!) < Definitions.Count - 1;

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetField(ref isBusy, value))
                    OnPropertyChanged(nameof(CanRefresh));
            }
        }

        public bool IsStale
        {
            get => isStale;
            private set => SetField(ref isStale, value);
        }

        public bool CanRefresh => !IsBusy;

        public string Status
        {
            get => status;
            private set => SetField(ref status, value);
        }

        public Task InitializeAsync()
        {
            if (initialized)
                return Task.CompletedTask;

            initialized = true;
            return LoadOnOpen ? RefreshAsync() : Task.CompletedTask;
        }

        public void NewStatistic()
        {
            StatisticDefinition definition = new();
            Definitions.Add(definition);
            SelectedDefinition = definition;
            collectionFile.NotifyMetadataChanged();
        }

        public void PrepareDraft(StatisticDefinition template)
        {
            DraftName = template.Name == "Empty" ? "New statistic" : template.Name;
            DraftSql = template.Sql;
        }

        public async Task AddDraftAsync()
        {
            StatisticDefinition definition = new()
            {
                Name = string.IsNullOrWhiteSpace(DraftName) ? "Unnamed statistic" : DraftName.Trim(),
                Sql = DraftSql,
            };
            Definitions.Add(definition);
            SelectedDefinition = definition;
            collectionFile.NotifyMetadataChanged();
            await RefreshAsync();
        }

        public async Task MoveSelectedAsync(int offset)
        {
            if (!HasSelectedDefinition || offset is not (-1 or 1))
                return;

            int index = Definitions.IndexOf(SelectedDefinition!);
            int target = index + offset;
            if (target < 0 || target >= Definitions.Count)
                return;

            Definitions.Move(index, target);
            NotifySelectionActions();
            collectionFile.NotifyMetadataChanged();
            await RefreshAsync();
        }

        public async Task AddDefaultAsync(StatisticDefinition template)
        {
            StatisticDefinition definition = template.Copy();
            Definitions.Add(definition);
            SelectedDefinition = definition;
            collectionFile.NotifyMetadataChanged();
            await RefreshAsync();
        }

        public async Task SaveAndRunAsync()
        {
            if (SelectedDefinition == null)
            {
                StatisticDefinition definition = new();
                Definitions.Add(definition);
                // Preserve the text entered before the first definition was created.
                selectedDefinition = definition;
                OnPropertyChanged(nameof(SelectedDefinition));
            }

            SelectedDefinition!.Name = string.IsNullOrWhiteSpace(EditorName) ? "Unnamed statistic" : EditorName.Trim();
            SelectedDefinition.Sql = EditorSql;
            collectionFile.NotifyMetadataChanged();
            await RefreshAsync();
        }

        public async Task DeleteSelectedAsync()
        {
            if (SelectedDefinition == null)
                return;

            int index = Definitions.IndexOf(SelectedDefinition);
            Definitions.Remove(SelectedDefinition);
            SelectedDefinition = Definitions.Count == 0 ? null : Definitions[Math.Min(index, Definitions.Count - 1)];
            collectionFile.NotifyMetadataChanged();
            await RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            if (disposed)
                return;

            refreshTimer.Stop();
            if (IsBusy)
            {
                refreshPending = true;
                return;
            }

            int startedRevision = revision;
            IsBusy = true;
            Status = "Refreshing statistics from the current datasource…";
            try
            {
                CollectionDataSnapshot snapshot = collectionFile.CreateStatisticsSnapshot();
                StatisticDefinition[] definitions = Definitions.Select(definition => new StatisticDefinition
                {
                    Id = definition.Id,
                    Name = definition.Name,
                    Sql = definition.Sql,
                }).ToArray();

                IReadOnlyList<StatisticReportResult> results =
                    await Task.Run(() => StatisticQueryService.ExecuteAll(snapshot, definitions));

                if (disposed || revision != startedRevision)
                    return;

                Results.Clear();
                foreach (StatisticReportResult result in results)
                    Results.Add(result);

                IsStale = false;
                int errors = results.Count(result => !string.IsNullOrWhiteSpace(result.Error));
                Status = results.Count == 0
                    ? "No statistics added. Choose a built-in statistic or create your own query."
                    : errors == 0
                        ? $"Statistics are up to date. Refreshed at {DateTime.Now:HH:mm:ss}."
                        : $"Refreshed statistics; {errors} query failed. See the messages above the tables.";
            }
            catch (Exception exception)
            {
                Status = $"Could not refresh statistics: {exception.Message}";
                IsStale = true;
            }
            finally
            {
                IsBusy = false;
                if (!disposed && (refreshPending || (IsStale && AutoRefresh && revision != startedRevision)))
                    QueueRefresh();

                refreshPending = false;
            }
        }

        private void DatasourceChanged(object? sender, EventArgs e)
        {
            revision++;
            MarkStale();
            if (AutoRefresh)
                QueueRefresh();
        }

        private void MetadataChanged(object? sender, EventArgs e)
        {
            string fingerprint = GetDefinitionFingerprint();
            bool definitionsChanged = fingerprint != definitionFingerprint;
            definitionFingerprint = fingerprint;
            NotifySettings();
            NotifySelectionActions();

            if (definitionsChanged)
            {
                revision++;
                MarkStale();
            }

            if (IsStale && AutoRefresh)
                QueueRefresh();
            else if (!AutoRefresh)
                refreshTimer.Stop();
        }

        private void MetadataReplaced(object? sender, EventArgs e)
        {
            revision++;
            refreshTimer.Stop();
            refreshPending = false;
            Results.Clear();
            definitionFingerprint = GetDefinitionFingerprint();
            SelectedDefinition = Definitions.FirstOrDefault();
            OnPropertyChanged(nameof(Definitions));
            NotifySettings();
            MarkStale();
            if (LoadOnOpen)
                QueueRefresh();
        }

        private void FileSaved(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(MetadataStatus));
            OnPropertyChanged(nameof(MetadataPath));
        }

        private void MarkStale()
        {
            IsStale = true;
            Status = "Statistics may be out of date. Select Refresh statistics to use the latest datasource.";
        }

        private void QueueRefresh()
        {
            refreshTimer.Stop();
            refreshTimer.Start();
        }

        private async void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            refreshTimer.Stop();
            await RefreshAsync();
        }

        private string GetDefinitionFingerprint() =>
            string.Join("\n", Definitions.Select(definition => $"{definition.Id}\0{definition.Name}\0{definition.Sql}"));

        private void NotifySettings()
        {
            OnPropertyChanged(nameof(IsJsonEnabled));
            OnPropertyChanged(nameof(LoadOnOpen));
            OnPropertyChanged(nameof(AutoRefresh));
            OnPropertyChanged(nameof(CanRefresh));
            OnPropertyChanged(nameof(MetadataStatus));
            OnPropertyChanged(nameof(MetadataPath));
        }

        private void NotifySelectionActions()
        {
            OnPropertyChanged(nameof(HasSelectedDefinition));
            OnPropertyChanged(nameof(CanMoveUp));
            OnPropertyChanged(nameof(CanMoveDown));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void Dispose()
        {
            disposed = true;
            refreshTimer.Stop();
            refreshTimer.Tick -= RefreshTimer_Tick;
            collectionFile.ContentChanged -= DatasourceChanged;
            collectionFile.MetadataChanged -= MetadataChanged;
            collectionFile.MetadataReplaced -= MetadataReplaced;
            collectionFile.FileSaved -= FileSaved;
        }
    }
}
