# Collection files and statistics

Records use a delimited text format, with the first line containing column names. Tab is the default; File information offers Tab, comma, semicolon and pipe delimiters. Values containing separators, quotes or line breaks are quoted. There is no CSV comment or inline metadata syntax. A normal record column named `Comments` remains ordinary record data.

Collection information lives in a sibling JSON file: `library.csv` uses `library.metadata.json`. Enable **Use JSON companion file** under the collection type in File information to create it immediately. New collections first open Save As to choose a filename. The companion path uses a bordered, selectable read-only text box. Opening a collection automatically loads an existing companion. Unticking the checkbox asks whether to delete the companion, keep it while disabling storage, or cancel. These actions preserve the live records and statistic definitions in memory. When JSON is off, statistics remain usable for the session and show a warning that queries will not be saved.

The delimiter is detected when no saved choice is available and is stored in JSON when enabled. Changing the dropdown affects subsequent saves of the current datasource; it does not reload the file or discard live edits.

The JSON contains application name/version, schema version, collection type, named SQL queries, and statistics preferences:

```json
{
  "applicationName": "Watched films tracker",
  "applicationVersion": "Version 0.15",
  "schemaVersion": 1,
  "collectionType": "Films",
  "delimiter": "\t",
  "statisticsSettings": {
    "loadOnOpen": true,
    "autoRefresh": false
  },
  "statistics": [
    {
      "id": "total-records",
      "name": "Total records",
      "sql": "SELECT COUNT(*) AS total FROM records"
    }
  ]
}
```

Unknown collection types are displayed by their stored name and use a generic icon and default columns. Their identity survives save/reopen. Unrecognized JSON properties and newer schema numbers are retained so an older application does not silently discard newer metadata.

## Responsibilities

| Location | Responsibility |
| --- | --- |
| `Source/Models` | Metadata, refresh preferences, immutable tabular snapshots |
| `Source/Persistence/Csv` | Read/write tabular records; no metadata or WPF dependencies |
| `Source/Persistence/Json` | Load/save JSON companions; preserve additional properties |
| `Source/Persistence/CollectionFileStore.cs` | Coordinate the two persistence formats |
| `Source/ManagingRecords` | Live editable records, schema changes and dirty tracking |
| `Source/Statistics` | SQL definitions, built-ins, results and DuckDB execution |
| `Source/ViewModels/StatisticsSession.cs` | Per-collection refresh state, preferences and stale results |
| `Source/Views/StatisticsPanel.xaml` | Statistics editor, controls and result tables |
| `Source/Views/CollectionGridFactory.cs` | Construct the record editor view |
| `Source/Views/MainWindow.xaml.cs` | Select the current collection and attach the panel |

Statistics capture **all live records**, including unsaved edits, from the datasource. They do not reread CSV files and do not depend on visual filtering, sorting or column display order. SQL runs against an in-memory DuckDB table called `records`. Columns are text; quote names containing spaces and use `TRY_CAST` for numeric/date calculations. Each query produces its own table or error message; output is limited to 1,000 rows. Queries are single `SELECT`/`WITH` statements with external file access disabled.

**Load statistics when opening** controls the initial calculation, including collections opened at application startup. **Refresh statistics automatically** recalculates after datasource edits, with a short debounce. When automatic refresh is disabled, edits mark results as out of date and the panel offers **Refresh statistics**. Tab switches reuse each collection's state. Refresh preferences are stored per collection in its companion file.

Enable **Reopen last collection** in the general startup settings to reopen the last saved collection on launch. That application preference is saved in `settings.conf`; the reopened collection's JSON determines whether its statistics are calculated immediately.

File information, Settings, Table structure and Collection statistics remember their expanded/collapsed state in program settings. Result tables appear first in Collection statistics, followed by an always-visible bordered Settings section. **Modify statistics** starts folded. Its existing-statistics list has move-up, move-down and delete controls; the separate **Add new statistics** form offers templates, name and SQL inputs. Choose **Empty** for an editable starter query. Moving definitions also changes their result-table order and the order saved in JSON.

## Verification

After building the solution, run these from the repository root:

```powershell
dotnet build WatchedFilmsTracker.sln --no-restore
pwsh -NoProfile -File Verification/VerifyPersistenceAndQueries.ps1
pwsh -NoProfile -STA -File Verification/VerifyStatisticsUi.ps1
pwsh -NoProfile -STA -File Verification/VerifyDelimiters.ps1
pwsh -NoProfile -STA -File Verification/VerifyMainWindow.ps1
pwsh -NoProfile -STA -File Verification/VerifyCompanionLifecycle.ps1
```

The scripts verify datasource queries, JSON persistence and delimiter round-trips, then exercise startup button states, panel preferences and statistics refresh policies with a WPF dispatcher. They do not open a visible application window.
