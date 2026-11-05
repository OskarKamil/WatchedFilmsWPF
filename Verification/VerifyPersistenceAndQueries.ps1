param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\bin\Debug\net8.0-windows7.0\WatchedFilmsTracker.dll')
)

$ErrorActionPreference = 'Stop'
$assemblyPathResolved = (Resolve-Path -LiteralPath $AssemblyPath).Path
$env:PATH = (Join-Path (Split-Path -Parent $assemblyPathResolved) 'runtimes\win-x64\native') + [IO.Path]::PathSeparator + $env:PATH
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPathResolved)

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    Write-Output "PASS: $Message"
}

function Invoke-Internal([string]$TypeName, [string]$Method, [object[]]$Arguments) {
    $type = $assembly.GetType($TypeName, $true)
    return $type.GetMethod($Method, [Reflection.BindingFlags]'Static,Public,NonPublic').Invoke($null, $Arguments)
}

$rows = [Collections.Generic.List[Collections.Generic.IEnumerable[string]]]::new()
$firstRow = [string[]]@('Film A', '1994')
$rows.Add($firstRow)
$rows.Add([string[]]@('Film B', 'invalid'))
$snapshot = [WatchedFilmsTracker.Source.Models.CollectionDataSnapshot]::new([string[]]@('Title', 'Release Year'), $rows)
$firstRow[1] = '2001'
Assert-True ($snapshot.Rows[0][1] -eq '1994') 'Snapshots preserve values when the live datasource subsequently changes.'

$definitions = [Collections.Generic.List[WatchedFilmsTracker.Source.Statistics.StatisticDefinition]]::new()
foreach ($sql in @(
    'SELECT COUNT(*) AS total FROM records',
    'SELECT SUM(TRY_CAST("Release Year" AS INTEGER)) AS total FROM records',
    'SELECT missing_column FROM records',
    'SELECT * FROM range(1200)',
    'DELETE FROM records',
    'SELECT 1; SELECT 2',
    "SELECT * FROM read_csv('unavailable.csv')"
)) {
    $definition = [WatchedFilmsTracker.Source.Statistics.StatisticDefinition]::new()
    $definition.Sql = $sql
    $definitions.Add($definition)
}
$arguments = [object[]]::new(2); $arguments[0] = $snapshot; $arguments[1] = $definitions
$results = Invoke-Internal 'WatchedFilmsTracker.Source.Statistics.StatisticQueryService' 'ExecuteAll' $arguments
Assert-True ($results[0].Rows[0]['total'] -eq 2) 'SQL counts rows in the supplied datasource snapshot.'
Assert-True ($results[1].Rows[0]['total'] -eq 1994) 'TRY_CAST tolerates invalid CSV numeric values.'
Assert-True (-not [string]::IsNullOrEmpty($results[2].Error)) 'A query error is reported without aborting other statistics.'
Assert-True ($results[3].Rows.Count -eq 1000) 'Result tables enforce the 1,000-row limit.'
Assert-True ($results[4].Error.Contains('SELECT or WITH')) 'Mutation statements are rejected.'
Assert-True ($results[5].Error.Contains('one SQL statement')) 'Multiple SQL statements are rejected.'
Assert-True (-not [string]::IsNullOrEmpty($results[6].Error)) 'Queries cannot read external CSV files.'

$testRoot = [string](Join-Path ([IO.Path]::GetTempPath()) ('WatchedFilmsTrackerPersistence-' + [Guid]::NewGuid().ToString('N')))
[void][IO.Directory]::CreateDirectory($testRoot)
$csvPath = [string](Join-Path $testRoot 'collection.csv')
$jsonPath = [IO.Path]::ChangeExtension($csvPath, '.metadata.json')
try {
    [IO.File]::WriteAllText($csvPath, "# Title`tRelease Year`r`n# Example`t2020`r`n")
    $reader = [Activator]::CreateInstance($assembly.GetType('WatchedFilmsTracker.Source.Persistence.Csv.CsvReader', $true))
    $data = $reader.Read($csvPath)
    Assert-True ($data.Columns[0] -eq '# Title' -and $data.Rows[0][0] -eq '# Example') 'CSV parsing has no comment syntax or metadata dependency.'
    [IO.File]::WriteAllText($jsonPath, @'
{
  "schemaVersion": 12,
  "collectionType": "Podcasts / Audio",
  "futureMetadata": { "preserve": true },
  "statisticsSettings": { "loadOnOpen": false, "autoRefresh": true, "futureSetting": 42 },
  "statistics": [{ "name": "Total", "sql": "SELECT COUNT(*) AS total FROM records", "futureStatistic": "keep" }]
}
'@)
    $metadata = Invoke-Internal 'WatchedFilmsTracker.Source.Persistence.Json.MetadataStore' 'Load' @($csvPath)
    Assert-True ($metadata.CollectionType -eq 'Podcasts / Audio' -and $metadata.StoreInJson) 'Unknown collection identities load without being replaced by Other.'
    $saveArguments = [object[]]::new(2); $saveArguments[0] = $csvPath; $saveArguments[1] = $metadata
    [void](Invoke-Internal 'WatchedFilmsTracker.Source.Persistence.Json.MetadataStore' 'Save' $saveArguments)
    $saved = [IO.File]::ReadAllText($jsonPath) | ConvertFrom-Json
    Assert-True ($saved.collectionType -eq 'Podcasts / Audio' -and $saved.schemaVersion -eq 12) 'Saving preserves unknown type names and newer schema versions.'
    Assert-True ($saved.futureMetadata.preserve -and $saved.statisticsSettings.futureSetting -eq 42 -and $saved.statistics[0].futureStatistic -eq 'keep') 'Future metadata, setting and statistic properties survive round-trip.'
    Assert-True (-not $saved.statisticsSettings.loadOnOpen -and $saved.statisticsSettings.autoRefresh) 'Per-collection refresh settings survive round-trip.'
    $before = [IO.File]::ReadAllText($jsonPath)
    $metadata.StoreInJson = $false
    [void](Invoke-Internal 'WatchedFilmsTracker.Source.Persistence.Json.MetadataStore' 'Save' $saveArguments)
    Assert-True ([IO.File]::ReadAllText($jsonPath) -eq $before) 'Disabling JSON leaves an existing companion untouched.'
    [IO.File]::WriteAllText($jsonPath, '{ broken json')
    $failedMetadata = Invoke-Internal 'WatchedFilmsTracker.Source.Persistence.Json.MetadataStore' 'Load' @($csvPath)
    Assert-True (-not [string]::IsNullOrEmpty($failedMetadata.LoadError)) 'Malformed JSON produces a useful load message.'
}
finally {
    $resolvedRoot = [IO.Path]::GetFullPath($testRoot)
    if (-not $resolvedRoot.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing cleanup outside the system temporary directory.'
    }
    foreach ($path in @($csvPath, $jsonPath)) {
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    Remove-Item -LiteralPath $resolvedRoot
}
