using System.IO;
using System.Text.Json;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Persistence.Json;

/// <summary>Reads and writes JSON companion files independently of record parsing.</summary>
internal static class MetadataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public static string GetSidecarPath(string csvPath) => Path.ChangeExtension(csvPath, ".metadata.json");

    public static Metadata Load(string csvPath)
    {
        string path = GetSidecarPath(csvPath);
        Metadata fallback = new() { SidecarPath = path };
        if (!File.Exists(path))
            return fallback;

        try
        {
            Metadata metadata = JsonSerializer.Deserialize<Metadata>(File.ReadAllText(path), JsonOptions)
                ?? throw new JsonException("Expected a metadata object.");
            metadata.SidecarPath = path;
            metadata.StoreInJson = true;
            metadata.CollectionType = string.IsNullOrWhiteSpace(metadata.CollectionType) ? "Other" : metadata.CollectionType;
            metadata.Statistics ??= new();
            metadata.RecentlyDeletedStatistics = new((metadata.RecentlyDeletedStatistics ?? new())
                .Where(entry => entry?.Definition != null)
                .OrderByDescending(entry => entry.DeletedAt)
                .Take(10));
            metadata.StatisticsSettings ??= new();
            // A hand-edited null entry is not a runnable statistic.
            for (int index = metadata.Statistics.Count - 1; index >= 0; index--)
                if (metadata.Statistics[index] is null)
                    metadata.Statistics.RemoveAt(index);
            if (metadata.SchemaVersion > Metadata.CurrentSchemaVersion)
                metadata.LoadError = $"Companion schema {metadata.SchemaVersion} is newer than supported schema {Metadata.CurrentSchemaVersion}. Unrecognized properties will be preserved.";
            return metadata;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            fallback.StoreInJson = true;
            fallback.LoadError = $"Could not load the JSON companion file: {exception.Message}";
            return fallback;
        }
    }

    public static void Save(string csvPath, Metadata metadata)
    {
        metadata.SidecarPath = GetSidecarPath(csvPath);
        if (!metadata.StoreInJson)
            return;

        metadata.ApplicationName = ProgramInformation.PROGRAM_NAME;
        metadata.ApplicationVersion = ProgramInformation.VERSION;
        metadata.SchemaVersion = Math.Max(metadata.SchemaVersion, Metadata.CurrentSchemaVersion);
        string json = JsonSerializer.Serialize(metadata, JsonOptions);
        string temporaryPath = metadata.SidecarPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, metadata.SidecarPath, overwrite: true);
            metadata.LoadError = string.Empty;
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
