namespace WatchedFilmsTracker.Source.Models
{
    /// <summary>
    /// An immutable copy of the live collection's column names and cell values.
    /// Keeps statistics independent of persistence and WPF controls, and permits
    /// query execution on a background thread while the collection is edited.
    /// </summary>
    public sealed class CollectionDataSnapshot
    {
        public CollectionDataSnapshot(IEnumerable<string> columns, IEnumerable<IEnumerable<string>> rows)
        {
            Columns = Array.AsReadOnly(columns.ToArray());
            Rows = Array.AsReadOnly(rows
                .Select(row => (IReadOnlyList<string>)Array.AsReadOnly(row.ToArray()))
                .ToArray());
        }

        public IReadOnlyList<string> Columns { get; }

        public IReadOnlyList<IReadOnlyList<string>> Rows { get; }
    }
}
