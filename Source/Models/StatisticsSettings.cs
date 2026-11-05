using System.Text.Json;
using System.Text.Json.Serialization;

namespace WatchedFilmsTracker.Source.Models;

/// <summary>Refresh preferences belonging to one collection, saved in its companion file.</summary>
public sealed class StatisticsSettings
{
    public bool LoadOnOpen { get; set; } = true;
    public bool AutoRefresh { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
