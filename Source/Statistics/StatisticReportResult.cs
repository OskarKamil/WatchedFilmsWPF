using System.Data;

namespace WatchedFilmsTracker.Source.Statistics
{
    public sealed class StatisticReportResult
    {
        public StatisticReportResult(string name, DataView rows, string error = "")
        {
            Name = name;
            Rows = rows;
            Error = error;
        }

        public string Error { get; }

        public string Name { get; }

        public DataView Rows { get; }
    }
}
