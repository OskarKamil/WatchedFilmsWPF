using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        private DeletedStatistic? selectedDeletedStatistic;
        private ObservableCollection<StatisticDefinition> subscribedDefinitions;
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
            subscribedDefinitions = Definitions;
            subscribedDefinitions.CollectionChanged += DefinitionsChanged;
            Results.CollectionChanged += ResultsChanged;
            collectionFile.ContentChanged += DatasourceChanged;
            collectionFile.MetadataChanged += MetadataChanged;
            collectionFile.MetadataReplaced += MetadataReplaced;
            collectionFile.FileSaved += FileSaved;
            SelectedDefinition = Definitions.FirstOrDefault();
            IsStale = HasStatistics;
            Status = "Statistics have not been loaded. Select Refresh statistics.";
        }

        public const string SessionOnlyWarningMessage =
            "Statistic queries are not saved and will disappear when the program closes. " +
            "To keep them, tick Use JSON companion file in File information and save the collection.";

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<StatisticReportResult> Results { get; } = new();
        public ObservableCollection<StatisticDefinition> Definitions => collectionFile.Metadata.Statistics;
        public bool HasStatistics => Definitions.Count > 0;
        public bool HasResults => Results.Count > 0;
        public IReadOnlyList<DeletedStatistic> RecentlyDeletedStatistics => collectionFile.Metadata.RecentlyDeletedStatistics
            .OrderByDescending(entry => entry.DeletedAt).ToArray();
        public bool HasDeletedStatistics => collectionFile.Metadata.RecentlyDeletedStatistics.Count > 0;
        public bool HasSelectedDeletedStatistic => SelectedDeletedStatistic != null &&
            collectionFile.Metadata.RecentlyDeletedStatistics.Contains(SelectedDeletedStatistic);

        public DeletedStatistic? SelectedDeletedStatistic
        {
            get => selectedDeletedStatistic;
            set
            {
                if (SetField(ref selectedDeletedStatistic, value))
                    OnPropertyChanged(nameof(HasSelectedDeletedStatistic));
            }
        }

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

                OnPropertyChanged(nameof(EditorName));
                OnPropertyChanged(nameof(EditorSql));
                NotifySelectionActions();
            }
        }

        public string EditorName
        {
            get => SelectedDefinition?.Name ?? string.Empty;
            set
            {
                if (!HasSelectedDefinition || EditorName == value)
                    return;

                SelectedDefinition!.Name = value;
                OnPropertyChanged();
                collectionFile.NotifyMetadataChanged();
            }
        }

        public string EditorSql
        {
            get => SelectedDefinition?.Sql ?? string.Empty;
            set
            {
                if (!HasSelectedDefinition || EditorSql == value)
                    return;

                SelectedDefinition!.Sql = value;
                OnPropertyChanged();
                collectionFile.NotifyMetadataChanged();
            }
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

        public bool CanRefresh => HasStatistics && !IsBusy;

        public string Status
        {
            get => HasStatistics ? status : "No statistics.";
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
            if (ReferenceEquals(template, DefaultStatisticsCatalog.Empty))
                definition.Name = "New statistic";
            Definitions.Add(definition);
            SelectedDefinition = definition;
            collectionFile.NotifyMetadataChanged();
            await RefreshAsync();
        }

        public async Task SaveAndRunAsync()
        {
            if (!HasSelectedDefinition)
                return;

            EditorName = string.IsNullOrWhiteSpace(EditorName) ? "Unnamed statistic" : EditorName.Trim();
            await RefreshAsync();
        }

        public async Task DeleteSelectedAsync()
        {
            if (!HasSelectedDefinition)
                return;

            int index = Definitions.IndexOf(SelectedDefinition!);
            collectionFile.Metadata.RecentlyDeletedStatistics.Insert(0, new DeletedStatistic
            {
                Definition = SelectedDefinition!,
                DeletedAt = DateTimeOffset.Now,
                OriginalIndex = index,
            });
            while (collectionFile.Metadata.RecentlyDeletedStatistics.Count > 10)
                collectionFile.Metadata.RecentlyDeletedStatistics.RemoveAt(collectionFile.Metadata.RecentlyDeletedStatistics.Count - 1);
            Definitions.Remove(SelectedDefinition!);
            SelectedDefinition = Definitions.Count == 0 ? null : Definitions[Math.Min(index, Definitions.Count - 1)];
            NotifyDeletedStatistics();
            collectionFile.NotifyMetadataChanged();
            await RefreshAsync();
        }

        public async Task RestoreSelectedAsync()
        {
            if (!HasSelectedDeletedStatistic)
                return;

            DeletedStatistic deleted = SelectedDeletedStatistic!;
            StatisticDefinition definition = deleted.Definition;
            if (Definitions.Any(existing => existing.Id == definition.Id))
                definition.Id = Guid.NewGuid().ToString("N");
            Definitions.Insert(Math.Clamp(deleted.OriginalIndex, 0, Definitions.Count), definition);
            collectionFile.Metadata.RecentlyDeletedStatistics.Remove(deleted);
            SelectedDefinition = definition;
            SelectedDeletedStatistic = RecentlyDeletedStatistics.FirstOrDefault();
            NotifyDeletedStatistics();
            collectionFile.NotifyMetadataChanged();
            await RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            if (disposed)
                return;

            refreshTimer.Stop();
            if (!HasStatistics)
            {
                Results.Clear();
                IsStale = false;
                refreshPending = false;
                return;
            }

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
                if (!disposed && HasStatistics && (refreshPending || (IsStale && AutoRefresh && revision != startedRevision)))
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
            subscribedDefinitions.CollectionChanged -= DefinitionsChanged;
            subscribedDefinitions = Definitions;
            subscribedDefinitions.CollectionChanged += DefinitionsChanged;
            definitionFingerprint = GetDefinitionFingerprint();
            SelectedDefinition = Definitions.FirstOrDefault();
            SelectedDeletedStatistic = null;
            OnPropertyChanged(nameof(Definitions));
            OnPropertyChanged(nameof(HasStatistics));
            OnPropertyChanged(nameof(Status));
            NotifyDeletedStatistics();
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
            IsStale = HasStatistics;
            Status = "Statistics may be out of date. Select Refresh statistics to use the latest datasource.";
        }

        private void QueueRefresh()
        {
            refreshTimer.Stop();
            if (HasStatistics)
                refreshTimer.Start();
        }

        private void DefinitionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            revision++;
            OnPropertyChanged(nameof(HasStatistics));
            OnPropertyChanged(nameof(CanRefresh));
            OnPropertyChanged(nameof(Status));
            if (!HasSelectedDefinition)
                SelectedDefinition = Definitions.FirstOrDefault();
            NotifySelectionActions();
            MarkStale();
            if (!HasStatistics)
            {
                refreshTimer.Stop();
                refreshPending = false;
                Results.Clear();
            }
        }

        private void ResultsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            OnPropertyChanged(nameof(HasResults));

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

        private void NotifyDeletedStatistics()
        {
            OnPropertyChanged(nameof(RecentlyDeletedStatistics));
            OnPropertyChanged(nameof(HasDeletedStatistics));
            OnPropertyChanged(nameof(HasSelectedDeletedStatistic));
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
            subscribedDefinitions.CollectionChanged -= DefinitionsChanged;
            Results.CollectionChanged -= ResultsChanged;
            collectionFile.ContentChanged -= DatasourceChanged;
            collectionFile.MetadataChanged -= MetadataChanged;
            collectionFile.MetadataReplaced -= MetadataReplaced;
            collectionFile.FileSaved -= FileSaved;
        }
    }
}
