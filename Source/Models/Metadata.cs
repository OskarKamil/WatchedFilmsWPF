using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using WatchedFilmsTracker.Source.Statistics;

namespace WatchedFilmsTracker.Source.Models;

/// <summary>Collection information persisted exclusively in the JSON companion file.</summary>
public sealed class Metadata
{
    public const int CurrentSchemaVersion = 1;

    public string ApplicationName { get; set; } = ProgramInformation.PROGRAM_NAME;
    public string ApplicationVersion { get; set; } = ProgramInformation.VERSION;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string CollectionType { get; set; } = "Other";
    public string? Delimiter { get; set; }
    public ObservableCollection<StatisticDefinition> Statistics { get; set; } = new();
    public ObservableCollection<DeletedStatistic> RecentlyDeletedStatistics { get; set; } = new();
    public StatisticsSettings StatisticsSettings { get; set; } = new();

    // Preserve properties written by newer application versions when saving again.
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

    [JsonIgnore]
    public string LoadError { get; internal set; } = string.Empty;
    [JsonIgnore]
    public string SidecarPath { get; internal set; } = string.Empty;
    [JsonIgnore]
    public bool StoreInJson { get; set; }
}
