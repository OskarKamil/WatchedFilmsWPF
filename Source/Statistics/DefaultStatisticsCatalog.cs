namespace WatchedFilmsTracker.Source.Statistics
{
    internal static class DefaultStatisticsCatalog
    {
        public static StatisticDefinition Empty { get; } = new()
        {
            Name = "Empty",
            Sql = "SELECT 'Edit me' AS \"Edit me\"",
        };

        public static IReadOnlyList<StatisticDefinition> All { get; } = new List<StatisticDefinition>
        {
            new()
            {
                Name = "Total records",
                Sql = "SELECT COUNT(*) AS \"Total records\" FROM records",
            },
            new()
            {
                Name = "Count by release year",
                Sql = """
                    WITH parsed AS (
                        SELECT TRY_CAST("Release Year" AS INTEGER) AS release_year
                        FROM records
                    )
                    SELECT release_year AS "Release year", COUNT(*) AS "Count"
                    FROM parsed
                    WHERE release_year IS NOT NULL
                    GROUP BY release_year
                    ORDER BY release_year
                    """,
            },
            new()
            {
                Name = "Count by release decade",
                Sql = """
                    WITH parsed AS (
                        SELECT TRY_CAST("Release Year" AS INTEGER) AS release_year
                        FROM records
                    )
                    SELECT FLOOR(release_year / 10) * 10 AS "Release decade", COUNT(*) AS "Count"
                    FROM parsed
                    WHERE release_year IS NOT NULL
                    GROUP BY "Release decade"
                    ORDER BY "Release decade"
                    """,
            },
            new()
            {
                Name = "Count by finish year",
                Sql = """
                    WITH parsed AS (
                        SELECT TRY_CAST(RIGHT("Finish Date", 4) AS INTEGER) AS finish_year
                        FROM records
                    )
                    SELECT finish_year AS "Finish year", COUNT(*) AS "Count"
                    FROM parsed
                    WHERE finish_year IS NOT NULL
                    GROUP BY finish_year
                    ORDER BY finish_year
                    """,
            },
            new()
            {
                Name = "Count by watched year (Watched In)",
                Sql = """
                    WITH parsed AS (
                        SELECT TRY_CAST("Watched In" AS INTEGER) AS watched_year
                        FROM records
                    )
                    SELECT watched_year AS "Watched year", COUNT(*) AS "Count"
                    FROM parsed
                    WHERE watched_year IS NOT NULL
                    GROUP BY watched_year
                    ORDER BY watched_year
                    """,
            },
            new()
            {
                Name = "Count by watched year (Watch date)",
                Sql = """
                    WITH parsed AS (
                        SELECT TRY_CAST(RIGHT("Watch date", 4) AS INTEGER) AS watched_year
                        FROM records
                    )
                    SELECT watched_year AS "Watched year", COUNT(*) AS "Count"
                    FROM parsed
                    WHERE watched_year IS NOT NULL
                    GROUP BY watched_year
                    ORDER BY watched_year
                    """,
            },
            Empty,
        };
    }
}
