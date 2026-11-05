param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\bin\Debug\net8.0-windows7.0\WatchedFilmsTracker.dll')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework, WindowsBase
[void][Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath).Path)

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    Write-Output "PASS: $Message"
}

function Assert-Throws([scriptblock]$Action, [string]$Message) {
    $thrown = $false
    try { & $Action } catch { $thrown = $true }
    Assert-True $thrown $Message
}

[WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadDefaultSettings()
$testRoot = [string](Join-Path ([IO.Path]::GetTempPath()) ('WatchedFilmsTrackerCompanion-' + [Guid]::NewGuid().ToString('N')))
[void][IO.Directory]::CreateDirectory($testRoot)
$csvPath = [string](Join-Path $testRoot 'collection.csv')
$jsonPath = [IO.Path]::ChangeExtension($csvPath, '.metadata.json')
$unrelatedPath = [string](Join-Path $testRoot 'notes.json')
$deferredCsvPath = [string](Join-Path $testRoot 'first save.csv')
$deferredJsonPath = [IO.Path]::ChangeExtension($deferredCsvPath, '.metadata.json')
$csvOnlyPath = [string](Join-Path $testRoot 'csv only.csv')
$csvOnlyJsonPath = [IO.Path]::ChangeExtension($csvOnlyPath, '.metadata.json')
try {
    [IO.File]::WriteAllText($csvPath, "Title`tRelease Year`r`nOriginal title`t2020`r`n")
    [IO.File]::WriteAllText($unrelatedPath, '{"keep":true}')
    $originalCsv = [Convert]::ToBase64String([IO.File]::ReadAllBytes($csvPath))
    $file = [WatchedFilmsTracker.Source.ManagingFilmsFile.CollectionFile]::new($csvPath)
    $file.SetCompanionStorage($true)
    Assert-True ([IO.File]::Exists($jsonPath) -and $file.Metadata.StoreInJson) 'Enabling JSON creates the companion immediately.'
    Assert-True ($file.CompanionFilePath -eq $jsonPath -and $file.CompanionFileExists) 'The collection exposes the exact companion path and existence.'
    Assert-True (-not $file.HasUnsavedChanges) 'Creating the companion does not make clean records dirty.'
    Assert-True ([Convert]::ToBase64String([IO.File]::ReadAllBytes($csvPath)) -eq $originalCsv) 'Immediate companion creation does not rewrite the CSV.'

    $file.CollectionOfRecords.ObservableCollectionOfRecords[0].Cells[1].Value = 'Unsaved title'
    $definition = [WatchedFilmsTracker.Source.Statistics.StatisticDefinition]::new()
    $definition.Name = 'Session query'
    $file.Metadata.Statistics.Add($definition)
    $file.NotifyMetadataChanged()
    $file.SetCompanionStorage($true)
    $saved = [IO.File]::ReadAllText($jsonPath) | ConvertFrom-Json
    Assert-True ($saved.statistics[0].name -eq 'Session query') 'Immediate creation writes the current metadata and statistic definitions.'
    Assert-True ($file.HasUnsavedChanges -and $file.CreateStatisticsSnapshot().Rows[0][1] -eq 'Unsaved title') 'Companion creation preserves unsaved datasource values and dirty state.'
    Assert-True ([Convert]::ToBase64String([IO.File]::ReadAllBytes($csvPath)) -eq $originalCsv) 'Unsaved record changes are not silently saved when enabling JSON.'

    $jsonBefore = [IO.File]::ReadAllText($jsonPath)
    $file.SetCompanionStorage($false, $false)
    Assert-True (-not $file.Metadata.StoreInJson -and [IO.File]::ReadAllText($jsonPath) -eq $jsonBefore) 'Disabling with Keep leaves the existing companion untouched.'
    Assert-True ($file.Metadata.Statistics.Count -eq 1) 'Disabling persistence retains statistics in memory.'

    $file.SetCompanionStorage($true)
    $lock = [IO.File]::Open($jsonPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        Assert-Throws { $file.SetCompanionStorage($false, $true) } 'A failed deletion is reported to the caller.'
        Assert-True $file.Metadata.StoreInJson 'A failed deletion leaves JSON persistence enabled.'
    }
    finally { $lock.Dispose() }

    $file.SetCompanionStorage($false, $true)
    Assert-True (-not $file.CompanionFileExists -and -not $file.Metadata.StoreInJson) 'Explicit Delete removes the companion and disables persistence.'
    Assert-True ([IO.File]::Exists($csvPath) -and [IO.File]::Exists($unrelatedPath)) 'Deleting the companion leaves the CSV and other JSON files untouched.'
    Assert-True ($file.Metadata.Statistics.Count -eq 1 -and $file.CreateStatisticsSnapshot().Rows[0][1] -eq 'Unsaved title') 'Deleting the companion preserves current statistics and live records.'

    [void][IO.Directory]::CreateDirectory($jsonPath)
    try {
        Assert-Throws { $file.SetCompanionStorage($true) } 'A blocked companion destination reports creation failure.'
        Assert-True (-not $file.Metadata.StoreInJson) 'Creation failure restores the previous persistence flag.'
    }
    finally { Remove-Item -LiteralPath $jsonPath }

    $newFile = [WatchedFilmsTracker.Source.ManagingFilmsFile.CollectionFile]::new(
        [WatchedFilmsTracker.Source.Models.CommonCollections]::GetCommonCollectionByName('Films'))
    $newFile.SetCompanionStorage($true)
    Assert-True ($newFile.Metadata.StoreInJson -and $newFile.HasUnsavedChanges) 'An unsaved collection remembers enabling JSON and becomes saveable.'
    Assert-True ($newFile.CompanionFilePath -eq '' -and -not [IO.File]::Exists($deferredJsonPath)) 'Enabling JSON without a CSV path does not create a file or invent a path.'
    $newFile.SaveFileAtLocation($deferredCsvPath)
    Assert-True ([IO.File]::Exists($deferredCsvPath) -and [IO.File]::Exists($deferredJsonPath)) 'First save creates the remembered JSON companion beside the chosen CSV.'
    Assert-True ($newFile.CompanionFilePath -eq $deferredJsonPath -and -not $newFile.HasUnsavedChanges) 'First save updates the companion path and clears dirty state.'

    $csvOnly = [WatchedFilmsTracker.Source.ManagingFilmsFile.CollectionFile]::new(
        [WatchedFilmsTracker.Source.Models.CommonCollections]::GetCommonCollectionByName('Films'))
    $csvOnly.SetCompanionStorage($true)
    $csvOnly.SetCompanionStorage($false)
    $csvOnly.SaveFileAtLocation($csvOnlyPath)
    Assert-True ([IO.File]::Exists($csvOnlyPath) -and -not [IO.File]::Exists($csvOnlyJsonPath)) 'Turning pending JSON off before first save creates only the CSV.'

    [WatchedFilmsTracker.Source.Managers.SettingsManager]::JsonForNewFiles = 'Yes'
    $alwaysJson = [WatchedFilmsTracker.Source.ManagingFilmsFile.CollectionFile]::new(
        [WatchedFilmsTracker.Source.Models.CommonCollections]::GetCommonCollectionByName('Films'))
    Assert-True ($alwaysJson.Metadata.StoreInJson -and $alwaysJson.CompanionFilePath -eq '') 'The Yes preference enables deferred JSON for new collections.'
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::JsonForNewFiles = 'No'
    $neverJson = [WatchedFilmsTracker.Source.ManagingFilmsFile.CollectionFile]::new(
        [WatchedFilmsTracker.Source.Models.CommonCollections]::GetCommonCollectionByName('Films'))
    Assert-True (-not $neverJson.Metadata.StoreInJson) 'The No preference leaves new collections as CSV only.'
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::JsonForNewFiles = 'AskEachTime'
    $askJson = [WatchedFilmsTracker.Source.ManagingFilmsFile.CollectionFile]::new(
        [WatchedFilmsTracker.Source.Models.CommonCollections]::GetCommonCollectionByName('Films'))
    Assert-True (-not $askJson.Metadata.StoreInJson) 'Ask each time waits for the new-collection action to supply the user choice.'
}
finally {
    $resolvedRoot = [IO.Path]::GetFullPath($testRoot)
    if (-not $resolvedRoot.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing cleanup outside the system temporary directory.'
    }
    foreach ($path in @($csvPath, $jsonPath, $unrelatedPath, $deferredCsvPath, $deferredJsonPath, $csvOnlyPath, $csvOnlyJsonPath)) {
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    Remove-Item -LiteralPath $resolvedRoot
}
