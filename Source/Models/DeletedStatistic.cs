using System.Text.Json;
using System.Text.Json.Serialization;
using WatchedFilmsTracker.Source.Statistics;

namespace WatchedFilmsTracker.Source.Models;

/// <summary>A recoverable statistic retained in the collection's companion file.</summary>
public sealed class DeletedStatistic
{
    public StatisticDefinition Definition { get; set; } = new();
    public DateTimeOffset DeletedAt { get; set; } = DateTimeOffset.Now;
    public int OriginalIndex { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

    [JsonIgnore]
    public string DisplayText
    {
        get
        {
            int days = Math.Max(0, (DateTime.Today - DeletedAt.LocalDateTime.Date).Days);
            string age = days == 0 ? "today" : days == 1 ? "1 day ago" : $"{days} days ago";
            return $"Deleted {age} - {Definition.Name}";
        }
    }
}
