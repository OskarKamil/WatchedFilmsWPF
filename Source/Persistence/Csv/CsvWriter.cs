using System.IO;
using System.Text;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Persistence.Csv;

/// <summary>Writes record data without any dependency on metadata or UI controls.</summary>
internal sealed class CsvWriter
{
    public void Write(string path, CollectionDataSnapshot data, string delimiter = "\t")
    {
        if (!DelimiterOptions.IsSupported(delimiter))
            throw new ArgumentException("Unsupported delimiter.", nameof(delimiter));

        // The generated row-number column is an application view detail.
        int[] indexes = Enumerable.Range(0, data.Columns.Count)
            .Where(index => data.Columns[index] != "#").ToArray();
        using StreamWriter writer = new(path, append: false, Encoding.UTF8) { NewLine = "\r\n" };
        writer.WriteLine(string.Join(delimiter, indexes.Select(index => Escape(data.Columns[index], delimiter))));
        foreach (IReadOnlyList<string> row in data.Rows)
            writer.WriteLine(string.Join(delimiter, indexes.Select(index => Escape(index < row.Count ? row[index] : string.Empty, delimiter))));
    }

    private static string Escape(string? value, string delimiter)
    {
        value ??= string.Empty;
        return value.Contains(delimiter) || value.Contains('"') || value.Contains('\r') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
