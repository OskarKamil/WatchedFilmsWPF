using WatchedFilmsTracker.Source.Models;
using WatchedFilmsTracker.Source.Persistence.Csv;
using WatchedFilmsTracker.Source.Persistence.Json;

namespace WatchedFilmsTracker.Source.Persistence;

/// <summary>Coordinates record and companion persistence; knows nothing about WPF.</summary>
internal sealed class CollectionFileStore
{
    private readonly CsvReader reader = new();
    private readonly CsvWriter writer = new();

    public (CollectionDataSnapshot Data, Metadata Metadata) Load(string path)
    {
        Metadata metadata = MetadataStore.Load(path);
        metadata.Delimiter = DelimiterOptions.IsSupported(metadata.Delimiter)
            ? metadata.Delimiter
            : reader.DetectDelimiter(path);
        return (reader.Read(path, metadata.Delimiter), metadata);
    }

    public void Save(string path, CollectionDataSnapshot data, Metadata metadata)
    {
        metadata.Delimiter = DelimiterOptions.IsSupported(metadata.Delimiter) ? metadata.Delimiter : "\t";
        writer.Write(path, data, metadata.Delimiter!);
        MetadataStore.Save(path, metadata);
    }
}
