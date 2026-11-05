param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\bin\Debug\net8.0-windows7.0\WatchedFilmsTracker.dll'),
    [string]$StartupDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework, WindowsBase
$assemblyPathResolved = (Resolve-Path -LiteralPath $AssemblyPath).Path
$binPath = Split-Path -Parent $assemblyPathResolved
$env:PATH = (Join-Path $binPath 'runtimes\win-x64\native') + [IO.Path]::PathSeparator + $env:PATH
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPathResolved)
[Windows.Application].GetField('_resourceAssembly', [Reflection.BindingFlags]'Static,NonPublic').SetValue($null, $assembly)
[Threading.SynchronizationContext]::SetSynchronizationContext(
    [Windows.Threading.DispatcherSynchronizationContext]::new([Windows.Threading.Dispatcher]::CurrentDispatcher))
$managerType = $assembly.GetType('WatchedFilmsTracker.Source.Managers.OpenedFilesManager', $true)

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    Write-Output "PASS: $Message"
}

function Pump-Dispatcher([int]$Milliseconds = 20) {
    $frame = [Windows.Threading.DispatcherFrame]::new()
    $timer = [Windows.Threading.DispatcherTimer]::new()
    $timer.Interval = [TimeSpan]::FromMilliseconds($Milliseconds)
    $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false }.GetNewClosure())
    $timer.Start()
    [Windows.Threading.Dispatcher]::PushFrame($frame)
}

function Invoke-Manager([string]$Name, [object[]]$Arguments) {
    return $managerType.GetMethod($Name, [Reflection.BindingFlags]'Static,Public,NonPublic').Invoke($null, $Arguments)
}

function Get-OpenedFiles {
    return ,$managerType.GetProperty('OpenedFiles', [Reflection.BindingFlags]'Static,Public,NonPublic').GetValue($null)
}

function Get-Tabs {
    return ,$managerType.GetProperty('TabsOfOpenedFiles', [Reflection.BindingFlags]'Static,Public,NonPublic').GetValue($null)
}

function Invoke-WindowHandler([string]$Name, [object[]]$Arguments) {
    [void]$window.GetType().GetMethod($Name, [Reflection.BindingFlags]'Instance,Public,NonPublic').Invoke($window, $Arguments)
}

function Reset-OpenedFiles {
    $window.FindName('CollectionStatisticsPanel').RaiseEvent(
        [Windows.RoutedEventArgs]::new([Windows.FrameworkElement]::UnloadedEvent))
    (Get-OpenedFiles).Clear()
    (Get-Tabs).Clear()
    Pump-Dispatcher
}

# The timer runs inside each real modal dialog's dispatcher loop. Any unexpected
# additional prompt is canceled so a failed assertion cannot hang the test host.
function Invoke-SaveSequence([string[]]$Choices, [scriptblock]$Action) {
    $state = @{ Paths = [Collections.Generic.List[string]]::new(); Unexpected = $false; Seen = $null }
    $timer = [Windows.Threading.DispatcherTimer]::new()
    $timer.Interval = [TimeSpan]::FromMilliseconds(20)
    $timer.Add_Tick({
        $dialog = @($window.OwnedWindows | Where-Object { $_ -is [WatchedFilmsTracker.Source.Views.SaveChangesDialog] })[0]
        if ($null -eq $dialog -or [object]::ReferenceEquals($state.Seen, $dialog)) { return }
        $state.Seen = $dialog
        $dialog.Opacity = 0
        $index = $state.Paths.Count
        $state.Paths.Add([string]$dialog.FindName('CollectionPathText').Text)
        $buttonName = if ($index -lt $Choices.Count) { $Choices[$index] } else { 'CancelButton' }
        if ($index -ge $Choices.Count) { $state.Unexpected = $true }
        $dialog.FindName($buttonName).RaiseEvent(
            [Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent))
    }.GetNewClosure())
    $timer.Start()
    try { & $Action }
    finally { $timer.Stop() }
    return $state
}

if (-not [string]::IsNullOrWhiteSpace($StartupDirectory)) {
    Set-Location -LiteralPath $StartupDirectory
    [Environment]::CurrentDirectory = $StartupDirectory
    $window = [WatchedFilmsTracker.MainWindow]::new()
    try {
        Pump-Dispatcher
        $restored = Get-OpenedFiles
        $expectedNames = @('first = collection.csv', 'second collection.csv', 'third collection.csv', 'fourth collection.csv')
        Assert-True ($restored.Count -eq 4) 'A fresh application process restores every saved tab from the previous session.'
        for ($index = 0; $index -lt $expectedNames.Count; $index++) {
            Assert-True ([IO.Path]::GetFileName($restored[$index].Filepath) -eq $expectedNames[$index]) "Startup preserves tab order at position $index."
        }
        Assert-True ([IO.Path]::GetFileName($window.CurrentWorkingFile().Filepath) -eq 'second collection.csv') 'Startup restores the previously active saved tab.'
    }
    finally {
        $window.FindName('CollectionStatisticsPanel').RaiseEvent(
            [Windows.RoutedEventArgs]::new([Windows.FrameworkElement]::UnloadedEvent))
    }
    return
}

$originalLocation = (Get-Location).Path
$originalDirectory = [Environment]::CurrentDirectory
$testRoot = [string](Join-Path ([IO.Path]::GetTempPath()) ('WatchedFilmsTrackerSession-' + [Guid]::NewGuid().ToString('N')))
[void][IO.Directory]::CreateDirectory($testRoot)
$paths = [string[]]@(
    (Join-Path $testRoot 'first = collection.csv'),
    (Join-Path $testRoot 'second collection.csv'),
    (Join-Path $testRoot 'third collection.csv'),
    (Join-Path $testRoot 'fourth collection.csv'))
$configPath = Join-Path $testRoot 'settings.conf'
$window = $null
try {
    Set-Location -LiteralPath $testRoot
    [Environment]::CurrentDirectory = $testRoot
    foreach ($path in $paths) { [IO.File]::WriteAllText($path, "Title,Release Year`r`nOriginal,2020`r`n") }

    [WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadDefaultSettings()
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::RestorePreviousSession = $true
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::SessionFilePaths = [string[]]@($paths[0], $paths[1], $paths[0].ToUpperInvariant(), '')
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::ActiveSessionFilePath = $paths[1]
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::SaveToConfFile()
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadDefaultSettings()
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadSettingsFromConfigFile()
    $loadedPaths = [WatchedFilmsTracker.Source.Managers.SettingsManager]::SessionFilePaths
    Assert-True ([WatchedFilmsTracker.Source.Managers.SettingsManager]::RestorePreviousSession -and $loadedPaths.Count -eq 2 -and
        $loadedPaths[0] -eq $paths[0] -and $loadedPaths[1] -eq $paths[1] -and
        [WatchedFilmsTracker.Source.Managers.SettingsManager]::ActiveSessionFilePath -eq $paths[1]) 'Settings round-trip the ordered, deduplicated file list and active path, including spaces and equals signs.'

    [WatchedFilmsTracker.Source.Managers.SettingsManager]::SessionFilePaths = [string[]]@()
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::LastPath = $paths[0]
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::SaveToConfFile()
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadDefaultSettings()
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadSettingsFromConfigFile()
    Assert-True ([WatchedFilmsTracker.Source.Managers.SettingsManager]::SessionFilePaths.Count -eq 0) 'An explicitly empty session does not fall back to the old last-file setting.'

    [IO.File]::WriteAllLines($configPath, [string[]]@('reopen_last_file = true', ('last_path = ' + $paths[0])))
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadDefaultSettings()
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadSettingsFromConfigFile()
    Assert-True ([WatchedFilmsTracker.Source.Managers.SettingsManager]::RestorePreviousSession -and
        [WatchedFilmsTracker.Source.Managers.SettingsManager]::SessionFilePaths.Count -eq 1 -and
        [WatchedFilmsTracker.Source.Managers.SettingsManager]::SessionFilePaths[0] -eq $paths[0]) 'Legacy single-file preferences migrate to session restore.'
    [IO.File]::WriteAllLines($configPath, [string[]]@('restore_previous_session = false', 'reopen_last_file = true', 'session_file_paths = not-json'))
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadDefaultSettings()
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadSettingsFromConfigFile()
    Assert-True (-not [WatchedFilmsTracker.Source.Managers.SettingsManager]::RestorePreviousSession -and
        [WatchedFilmsTracker.Source.Managers.SettingsManager]::SessionFilePaths.Count -eq 0) 'The new preference takes precedence and a malformed session list is ignored safely.'
    [IO.File]::Delete($configPath)

    $window = [WatchedFilmsTracker.MainWindow]::new()
    $window.Opacity = 0
    $window.ShowActivated = $false
    $window.ShowInTaskbar = $false
    $window.Show()
    Invoke-Manager 'CreateNewWorkingFile' @($paths[0])
    Invoke-Manager 'CreateNewWorkingFile' @($paths[1])
    $opened = Get-OpenedFiles
    $first = $opened[0]
    $second = $opened[1]
    $first.CollectionOfRecords.ObservableCollectionOfRecords[0].Cells[1].Value = 'Changed first'
    Invoke-Manager 'CreateNewWorkingFile' @([IO.Path]::GetFileName($paths[0]))
    Invoke-Manager 'CreateNewWorkingFile' @($paths[0].ToUpperInvariant())
    Assert-True ($opened.Count -eq 2 -and [object]::ReferenceEquals($window.CurrentWorkingFile(), $first) -and
        $first.CollectionOfRecords.ObservableCollectionOfRecords[0].Cells[1].Value -eq 'Changed first' -and $first.HasUnsavedChanges) 'Relative and case-variant duplicate opens activate the existing dirty tab without reloading it.'
    [IO.File]::Delete($paths[0])
    Invoke-Manager 'CreateNewWorkingFile' @($paths[0])
    Assert-True ($opened.Count -eq 2 -and [object]::ReferenceEquals($window.CurrentWorkingFile(), $first)) 'An already open path remains a duplicate even if its file was removed from disk.'
    [IO.File]::WriteAllText($paths[0], "Title,Release Year`r`nOriginal,2020`r`n")

    $films = [WatchedFilmsTracker.Source.Models.CommonCollections]::GetCommonCollectionByName('Films')
    Invoke-Manager 'CreateEmptyWorkingFile' @($films)
    $firstDraft = $opened[$opened.Count - 1]
    Invoke-Manager 'CreateEmptyWorkingFile' @($films)
    $secondDraft = $opened[$opened.Count - 1]
    Assert-True ($opened.Count -eq 4 -and $firstDraft.Filepath -eq '' -and $secondDraft.Filepath -eq '' -and
        -not [object]::ReferenceEquals($firstDraft, $secondDraft)) 'Multiple unsaved collections without paths can remain open.'
    $firstBytes = [Convert]::ToBase64String([IO.File]::ReadAllBytes($paths[0]))
    try { $firstDraft.SaveFileAtLocation($paths[0]) }
    catch [InvalidOperationException] { }
    Assert-True ($firstDraft.Filepath -eq '' -and [Convert]::ToBase64String([IO.File]::ReadAllBytes($paths[0])) -eq $firstBytes) 'Saving another tab to an open path is rejected before overwriting the CSV.'

    Invoke-Manager 'CreateNewWorkingFile' @($paths[2])
    Invoke-Manager 'CreateNewWorkingFile' @($paths[3])
    $third = $opened[4]
    $fourth = $opened[5]
    foreach ($file in @($second, $third, $fourth)) {
        $file.CollectionOfRecords.ObservableCollectionOfRecords[0].Cells[1].Value = 'Changed ' + [IO.Path]::GetFileName($file.Filepath)
    }
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::AutoSave = $true
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::RestorePreviousSession = $true
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::SessionFilePaths = [string[]]@($paths[3])
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::ActiveSessionFilePath = $paths[3]
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::SaveToConfFile()
    $configBeforeCancel = [IO.File]::ReadAllText($configPath)
    $diskBefore = @{}
    foreach ($path in $paths) { $diskBefore[$path] = [IO.File]::ReadAllText($path) }
    Invoke-Manager 'SelectFile' @($second)
    $closeArgs = [ComponentModel.CancelEventArgs]::new()
    $sequence = Invoke-SaveSequence @('SaveButton', 'DiscardButton', 'CancelButton') {
        Invoke-WindowHandler 'MainWindow_Closing' @($window, $closeArgs)
    }
    Assert-True ($closeArgs.Cancel -and $sequence.Paths.Count -eq 3 -and -not $sequence.Unexpected) 'Cancel stops shutdown immediately after the requested Save and Discard decisions, even when autosave is enabled.'
    Assert-True ($sequence.Paths[0].Contains([IO.Path]::GetFileName($paths[0])) -and
        $sequence.Paths[1].Contains([IO.Path]::GetFileName($paths[1])) -and
        $sequence.Paths[2].Contains([IO.Path]::GetFileName($paths[2]))) 'Each save prompt identifies the file being checked, in tab order.'
    Assert-True ([IO.File]::ReadAllText($paths[0]).Contains('Changed first') -and -not $first.HasUnsavedChanges) 'Save writes the first file before checking the next file.'
    Assert-True ([IO.File]::ReadAllText($paths[1]) -eq $diskBefore[$paths[1]] -and $second.HasUnsavedChanges -and
        [IO.File]::ReadAllText($paths[2]) -eq $diskBefore[$paths[2]] -and $third.HasUnsavedChanges -and
        [IO.File]::ReadAllText($paths[3]) -eq $diskBefore[$paths[3]] -and $fourth.HasUnsavedChanges) 'Discard followed by Cancel preserves unsaved in-memory edits and leaves later files untouched.'
    Assert-True ($opened.Count -eq 6 -and (Get-Tabs).Count -eq 6 -and
        [IO.File]::ReadAllText($configPath) -eq $configBeforeCancel) 'Cancel retains every tab and leaves the previous saved session unchanged.'

    Invoke-Manager 'SelectFile' @($second)
    $completeArgs = [ComponentModel.CancelEventArgs]::new()
    $sequence = Invoke-SaveSequence @('DiscardButton', 'DiscardButton', 'DiscardButton') {
        Invoke-WindowHandler 'MainWindow_Closing' @($window, $completeArgs)
    }
    $sessionPaths = [WatchedFilmsTracker.Source.Managers.SettingsManager]::SessionFilePaths
    Assert-True (-not $completeArgs.Cancel -and $sequence.Paths.Count -eq 3 -and -not $sequence.Unexpected -and
        $opened.Count -eq 6 -and (Get-Tabs).Count -eq 6) 'A successful shutdown check skips clean files and leaves tabs intact until the window closes.'
    Assert-True ($sessionPaths.Count -eq 4 -and ($sessionPaths -join "`n") -eq ($paths -join "`n") -and
        [WatchedFilmsTracker.Source.Managers.SettingsManager]::ActiveSessionFilePath -eq $paths[1]) 'Closing remembers all saved paths in order and the active tab, excluding untitled drafts.'
    foreach ($path in $paths[1..3]) {
        Assert-True ([IO.File]::ReadAllText($path) -eq $diskBefore[$path]) 'Do not save leaves that file unchanged on disk.'
    }

    & (Get-Process -Id $PID).Path -NoProfile -STA -File $PSCommandPath -AssemblyPath $assemblyPathResolved -StartupDirectory $testRoot
    Assert-True ($LASTEXITCODE -eq 0) 'Session restoration succeeds in a separate fresh process.'

    Reset-OpenedFiles
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::RestorePreviousSession = $true
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::SessionFilePaths = [string[]]@($paths[0], (Join-Path $testRoot 'missing.csv'), $paths[1], $paths[0].ToUpperInvariant(), $paths[2], $paths[3])
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::ActiveSessionFilePath = $paths[1]
    Invoke-WindowHandler 'RestorePreviousSession' @()
    Invoke-WindowHandler 'RestorePreviousSession' @()
    Assert-True ((Get-OpenedFiles).Count -eq 4 -and [object]::ReferenceEquals($window.CurrentWorkingFile(), (Get-OpenedFiles)[1])) 'Restore skips missing files, preserves active selection, and avoids duplicate tabs when repeated.'
    Reset-OpenedFiles
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::RestorePreviousSession = $false
    Invoke-WindowHandler 'RestorePreviousSession' @()
    Assert-True ((Get-OpenedFiles).Count -eq 0) 'Turning off session restore prevents startup files from opening.'
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::RestorePreviousSession = $true
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::SessionFilePaths = [string[]]@()
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::LastPath = $paths[0]
    Invoke-WindowHandler 'RestorePreviousSession' @()
    Assert-True ((Get-OpenedFiles).Count -eq 0) 'An empty saved session restores no files even when an old last path is present.'
}
finally {
    if ($null -ne $window) {
        $window.FindName('CollectionStatisticsPanel').RaiseEvent(
            [Windows.RoutedEventArgs]::new([Windows.FrameworkElement]::UnloadedEvent))
        $window.Hide()
    }
    Set-Location -LiteralPath $originalLocation
    [Environment]::CurrentDirectory = $originalDirectory
    $resolvedRoot = [IO.Path]::GetFullPath($testRoot)
    if (-not $resolvedRoot.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing cleanup outside the system temporary directory.'
    }
    foreach ($path in $paths + @($configPath)) {
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    Remove-Item -LiteralPath $resolvedRoot
}
