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
    Assert-Throws { $newFile.SetCompanionStorage($true) } 'An unsaved collection must choose its CSV path before companion creation.'
    Assert-True (-not $newFile.Metadata.StoreInJson -and $newFile.CompanionFilePath -eq '') 'No-path failure does not enable persistence or invent a companion path.'
}
finally {
    $resolvedRoot = [IO.Path]::GetFullPath($testRoot)
    if (-not $resolvedRoot.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing cleanup outside the system temporary directory.'
    }
    foreach ($path in @($csvPath, $jsonPath, $unrelatedPath)) {
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    Remove-Item -LiteralPath $resolvedRoot
}
