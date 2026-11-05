using System.IO;
using System.Text;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Persistence.Csv;

/// <summary>Reads delimited records, including quoted delimiters, quotes and line breaks.</summary>
internal sealed class CsvReader
{
    public CollectionDataSnapshot Read(string path, string? delimiter = null)
    {
        delimiter ??= DetectDelimiter(path);
        if (!DelimiterOptions.IsSupported(delimiter))
            throw new ArgumentException("Unsupported delimiter.", nameof(delimiter));

        using StreamReader reader = new(path);
        List<List<string>> records = ReadRecords(reader, delimiter[0]).ToList();
        if (records.Count == 0)
            return new CollectionDataSnapshot(Array.Empty<string>(), Array.Empty<string[]>());

        List<string> columns = records[0];
        List<List<string>> rows = records.Skip(1).ToList();
        int width = records.Max(record => record.Count);
        while (columns.Count < width)
            columns.Add(string.Empty);
        foreach (List<string> row in rows)
            while (row.Count < width)
                row.Add(string.Empty);

        return new CollectionDataSnapshot(columns, rows);
    }

    public string DetectDelimiter(string path)
    {
        using StreamReader reader = new(path);
        Dictionary<char, int> counts = DelimiterOptions.All.ToDictionary(option => option.Value[0], _ => 0);
        bool quoted = false;
        int code;
        while ((code = reader.Read()) >= 0)
        {
            char character = (char)code;
            if (character == '"')
            {
                if (quoted && reader.Peek() == '"')
                    reader.Read();
                else
                    quoted = !quoted;
            }
            else if (!quoted)
            {
                if (character is '\r' or '\n')
                    break;
                if (counts.ContainsKey(character))
                    counts[character]++;
            }
        }

        // Tab wins ties, preserving the existing format for single-column files.
        return DelimiterOptions.All.OrderByDescending(option => counts[option.Value[0]]).First().Value;
    }

    private static IEnumerable<List<string>> ReadRecords(TextReader reader, char delimiter)
    {
        List<string> fields = new();
        StringBuilder field = new();
        bool quoted = false;
        bool hasContent = false;
        int code;
        while ((code = reader.Read()) >= 0)
        {
            char character = (char)code;
            hasContent = true;
            if (quoted)
            {
                if (character == '"')
                {
                    if (reader.Peek() == '"')
                    {
                        reader.Read();
                        field.Append('"');
                    }
                    else
                        quoted = false;
                }
                else
                    field.Append(character);
            }
            else if (character == '"' && field.Length == 0)
                quoted = true;
            else if (character == delimiter)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else if (character is '\r' or '\n')
            {
                if (character == '\r' && reader.Peek() == '\n')
                    reader.Read();
                fields.Add(field.ToString());
                yield return fields;
                fields = new();
                field.Clear();
                hasContent = false;
            }
            else
                field.Append(character);
        }

        if (quoted)
            throw new FormatException("The collection contains an unterminated quoted field.");
        if (hasContent)
        {
            fields.Add(field.ToString());
            yield return fields;
        }
    }
}
