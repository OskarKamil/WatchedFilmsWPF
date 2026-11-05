using DuckDB.NET.Data;
using System.Data;
using System.Data.Common;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Statistics
{
    internal static class StatisticQueryService
    {
        private const int MaximumResultRows = 1_000;

        public static IReadOnlyList<StatisticReportResult> ExecuteAll(
            CollectionDataSnapshot data,
            IEnumerable<StatisticDefinition> statisticDefinitions)
        {
            List<StatisticDefinition> definitions = statisticDefinitions
                .Select(definition => new StatisticDefinition
                {
                    Id = definition.Id,
                    Name = definition.Name,
                    Sql = definition.Sql,
                })
                .ToList();
            if (definitions.Count == 0)
                return Array.Empty<StatisticReportResult>();

            try
            {
                using DuckDBConnection connection = new("Data Source=:memory:");
                connection.Open();
                CreateRecordsTable(connection, data);
                DisableExternalAccess(connection);

                return definitions
                    .Select(definition => Execute(connection, definition))
                    .ToList();
            }
            catch (Exception exception)
            {
                return definitions
                    .Select(definition => ErrorResult(definition.Name, exception.Message))
                    .ToList();
            }
        }

        private static void CreateRecordsTable(DuckDBConnection connection, CollectionDataSnapshot data)
        {
            List<string> columnNames = GetUniqueColumnNames(data.Columns);
            if (columnNames.Count == 0)
                columnNames.Add("Value");

            using DuckDBCommand createCommand = connection.CreateCommand();
            createCommand.CommandText =
                $"CREATE TABLE records ({string.Join(", ", columnNames.Select(name => $"{QuoteIdentifier(name)} VARCHAR"))})";
            createCommand.ExecuteNonQuery();

            using DuckDBTransaction transaction = connection.BeginTransaction();
            using DuckDBCommand insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText =
                $"INSERT INTO records VALUES ({string.Join(", ", columnNames.Select(_ => "?"))})";

            DbParameter[] parameters = columnNames
                .Select(_ =>
                {
                    DbParameter parameter = insertCommand.CreateParameter();
                    insertCommand.Parameters.Add(parameter);
                    return parameter;
                })
                .ToArray();

            foreach (IReadOnlyList<string> row in data.Rows)
            {
                for (int index = 0; index < parameters.Length; index++)
                {
                    parameters[index].Value = index < row.Count
                        ? (object?)row[index] ?? DBNull.Value
                        : DBNull.Value;
                }

                insertCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static void DisableExternalAccess(DuckDBConnection connection)
        {
            using DuckDBCommand command = connection.CreateCommand();
            command.CommandText = "SET enable_external_access = false";
            command.ExecuteNonQuery();
        }

        private static StatisticReportResult Execute(
            DuckDBConnection connection,
            StatisticDefinition definition)
        {
            try
            {
                string query = ValidateAndNormalizeQuery(definition.Sql);

                using DuckDBCommand command = connection.CreateCommand();
                command.CommandText =
                    $"SELECT * FROM ({query}) AS statistic_result LIMIT {MaximumResultRows}";

                using DbDataReader reader = command.ExecuteReader();
                DataTable table = ReadTable(reader);
                return new StatisticReportResult(definition.Name, table.DefaultView);
            }
            catch (Exception exception)
            {
                return ErrorResult(definition.Name, exception.Message);
            }
        }

        private static List<string> GetUniqueColumnNames(IEnumerable<string> columns)
        {
            HashSet<string> usedNames = new(StringComparer.OrdinalIgnoreCase);
            List<string> names = new();

            foreach (string column in columns)
            {
                string baseName = column?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(baseName))
                    baseName = $"Column {names.Count + 1}";

                string uniqueName = baseName;
                int suffix = 2;
                while (!usedNames.Add(uniqueName))
                    uniqueName = $"{baseName}_{suffix++}";

                names.Add(uniqueName);
            }

            return names;
        }

        private static string QuoteIdentifier(string identifier) =>
            $"\"{identifier.Replace("\"", "\"\"")}\"";

        private static string ValidateAndNormalizeQuery(string sql)
        {
            string query = sql.Trim();
            if (query.EndsWith(';'))
                query = query[..^1].TrimEnd();

            if (string.IsNullOrWhiteSpace(query))
                throw new InvalidOperationException("Enter a SQL query first.");

            if (query.Contains(';'))
                throw new InvalidOperationException("Only one SQL statement can be run at a time.");

            string firstKeyword = query
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? string.Empty;

            if (!firstKeyword.Equals("SELECT", StringComparison.OrdinalIgnoreCase) &&
                !firstKeyword.Equals("WITH", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Statistics must be a SELECT or WITH query.");
            }

            return query;
        }

        private static DataTable ReadTable(DbDataReader reader)
        {
            DataTable table = new();
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < reader.FieldCount; index++)
            {
                string baseName = reader.GetName(index);
                if (string.IsNullOrWhiteSpace(baseName))
                    baseName = $"Column {index + 1}";

                string name = baseName;
                int suffix = 2;
                while (!names.Add(name))
                    name = $"{baseName}_{suffix++}";

                table.Columns.Add(name, typeof(object));
            }

            while (reader.Read())
            {
                DataRow row = table.NewRow();
                for (int index = 0; index < reader.FieldCount; index++)
                    row[index] = reader.IsDBNull(index) ? DBNull.Value : reader.GetValue(index);

                table.Rows.Add(row);
            }

            return table;
        }

        private static StatisticReportResult ErrorResult(string name, string error)
        {
            DataTable emptyTable = new();
            return new StatisticReportResult(name, emptyTable.DefaultView, error);
        }
    }
}
