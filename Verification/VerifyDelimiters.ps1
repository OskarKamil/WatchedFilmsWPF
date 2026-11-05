param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\bin\Debug\net8.0-windows7.0\WatchedFilmsTracker.dll')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework, WindowsBase
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath).Path)

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    Write-Output "PASS: $Message"
}

$reader = [Activator]::CreateInstance($assembly.GetType('WatchedFilmsTracker.Source.Persistence.Csv.CsvReader', $true))
$writer = [Activator]::CreateInstance($assembly.GetType('WatchedFilmsTracker.Source.Persistence.Csv.CsvWriter', $true))
$store = [Activator]::CreateInstance($assembly.GetType('WatchedFilmsTracker.Source.Persistence.CollectionFileStore', $true))
$testRoot = [string](Join-Path ([IO.Path]::GetTempPath()) ('WatchedFilmsTrackerDelimiters-' + [Guid]::NewGuid().ToString('N')))
[void][IO.Directory]::CreateDirectory($testRoot)
$path = [string](Join-Path $testRoot 'collection.csv')
$jsonPath = [IO.Path]::ChangeExtension($path, '.metadata.json')
try {
    $rows = [Collections.Generic.List[Collections.Generic.IEnumerable[string]]]::new()
    $rows.Add([string[]]@('1', '  spaces preserved  ', "Comma, semicolon; pipe| and tab`t", "a `"quote`"", "first`r`nsecond", ''))
    $snapshot = [WatchedFilmsTracker.Source.Models.CollectionDataSnapshot]::new(
        [string[]]@('#', 'Title', 'Punctuation', 'Quote', 'Multiline', 'Empty'), $rows)
    foreach ($option in [WatchedFilmsTracker.Source.Models.DelimiterOptions]::All) {
        $writer.Write($path, $snapshot, $option.Value)
        $data = $reader.Read($path)
        Assert-True ($reader.DetectDelimiter($path) -eq $option.Value) "$($option.DisplayName): detects the saved delimiter."
        Assert-True ($data.Columns.Count -eq 5 -and $data.Columns[0] -eq 'Title') "$($option.DisplayName): excludes the generated row ID column."
        Assert-True ($data.Rows[0][0] -eq '  spaces preserved  ' -and $data.Rows[0][1] -eq "Comma, semicolon; pipe| and tab`t") "$($option.DisplayName): round-trips spaces and embedded delimiters."
        Assert-True ($data.Rows[0][2] -eq "a `"quote`"" -and $data.Rows[0][3] -eq "first`r`nsecond" -and $data.Rows[0][4] -eq '') "$($option.DisplayName): round-trips escaped quotes, multiline and empty fields."
    }

    [WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadDefaultSettings()
    $file = [WatchedFilmsTracker.Source.ManagingFilmsFile.CollectionFile]::new($path)
    $before = $file.CreateStatisticsSnapshot()
    $file.Delimiter = ';'
    $after = $file.CreateStatisticsSnapshot()
    Assert-True ($after.Rows.Count -eq $before.Rows.Count -and $after.Rows[0][1] -eq $before.Rows[0][1]) 'Changing the delimiter preserves live datasource values.'
    Assert-True $file.HasUnsavedChanges 'Changing the delimiter marks the collection unsaved.'
    $file.Metadata.StoreInJson = $true
    [void]$file.Save()
    $saved = [IO.File]::ReadAllText($jsonPath) | ConvertFrom-Json
    Assert-True ($saved.delimiter -eq ';') 'Selected delimiter is stored in the JSON companion.'
    $reopened = [WatchedFilmsTracker.Source.ManagingFilmsFile.CollectionFile]::new($path)
    Assert-True ($reopened.Delimiter -eq ';' -and $reopened.CreateStatisticsSnapshot().Rows[0][1] -eq $before.Rows[0][1]) 'Reopening honors the saved delimiter and record data.'
}
finally {
    $resolvedRoot = [IO.Path]::GetFullPath($testRoot)
    if (-not $resolvedRoot.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing cleanup outside the system temporary directory.'
    }
    foreach ($filePath in @($path, $jsonPath)) {
        if (Test-Path -LiteralPath $filePath) { Remove-Item -LiteralPath $filePath }
    }
    Remove-Item -LiteralPath $resolvedRoot
}
