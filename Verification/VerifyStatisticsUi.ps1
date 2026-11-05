param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\bin\Debug\net8.0-windows7.0\WatchedFilmsTracker.dll')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework, WindowsBase
$assemblyPathResolved = (Resolve-Path -LiteralPath $AssemblyPath).Path
$binPath = Split-Path -Parent $assemblyPathResolved
$env:PATH = (Join-Path $binPath 'runtimes\win-x64\native') + [IO.Path]::PathSeparator + $env:PATH
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPathResolved)
[Threading.SynchronizationContext]::SetSynchronizationContext(
    [Windows.Threading.DispatcherSynchronizationContext]::new([Windows.Threading.Dispatcher]::CurrentDispatcher))

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

function Wait-UiTask($Task) {
    $timeout = [Diagnostics.Stopwatch]::StartNew()
    while (-not $Task.IsCompleted) {
        if ($timeout.Elapsed.TotalSeconds -gt 30) { throw 'Statistics query timed out in verification.' }
        Pump-Dispatcher
    }
    [void]$Task.GetAwaiter().GetResult()
}

function New-Session($File) {
    $sessionType = $assembly.GetType('WatchedFilmsTracker.Source.ViewModels.StatisticsSession', $true)
    $arguments = [object[]]::new(1)
    $arguments[0] = $File
    return [Activator]::CreateInstance($sessionType, [Reflection.BindingFlags]'Instance,Public,NonPublic',
        $null, $arguments, $null)
}

[WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadDefaultSettings()
$type = [WatchedFilmsTracker.Source.Models.CommonCollections]::GetCommonCollectionByName('Films')
$file = [WatchedFilmsTracker.Source.ManagingFilmsFile.CollectionFile]::new($type)
$cells = [Collections.Generic.List[WatchedFilmsTracker.Source.ManagingRecords.Cell]]::new()
foreach ($value in @('1', 'Film A', 'Original', 'Feature', '1994', '8', '01/01/2024', '2024', '', '')) {
    $cells.Add([WatchedFilmsTracker.Source.ManagingRecords.Cell]::new($value))
}
$file.CollectionOfRecords.ObservableCollectionOfRecords.Add(
    [WatchedFilmsTracker.Source.ManagingFilmsFile.RecordModel]::new($cells))
$definition = [WatchedFilmsTracker.Source.Statistics.StatisticDefinition]::new()
$definition.Sql = 'SELECT "Release Year" AS year FROM records'
$file.Metadata.Statistics.Add($definition)
$file.Metadata.StoreInJson = $true
$file.Metadata.StatisticsSettings.LoadOnOpen = $false
$session = New-Session $file

try {
    Wait-UiTask ($session.InitializeAsync())
    Assert-True ($session.Results.Count -eq 0 -and $session.IsStale) 'LoadOnOpen=false leaves queries unloaded.'
    Wait-UiTask ($session.RefreshAsync())
    Assert-True ($session.Results[0].Rows[0]['year'] -eq '1994') 'Manual refresh queries the live datasource.'
    $cells[4].Value = '2001'
    Assert-True ($session.IsStale -and $session.Status.Contains('out of date')) 'Unsaved edits mark manual statistics stale.'
    Assert-True ($session.Results[0].Rows[0]['year'] -eq '1994') 'Manual mode retains old results until refresh.'
    Wait-UiTask ($session.InitializeAsync())
    Assert-True $session.IsStale 'Switching back to a cached session does not rerun unloaded or stale statistics.'
    Wait-UiTask ($session.RefreshAsync())
    Assert-True ($session.Results[0].Rows[0]['year'] -eq '2001') 'Refresh includes unsaved cell edits.'

    $file.GetDataGridManager().RenameColumnAt(4, 'Published')
    $definition.Sql = 'SELECT "Published" AS year FROM records'
    $file.NotifyMetadataChanged()
    Wait-UiTask ($session.RefreshAsync())
    Assert-True ($session.Results[0].Rows[0]['year'] -eq '2001') 'Queries see unsaved schema renames.'

    $session.AutoRefresh = $true
    $cells[4].Value = '2002'
    $cells[4].Value = '2003'
    Assert-True $session.IsStale 'Auto refresh immediately marks cached results stale.'
    Pump-Dispatcher 750
    $timeout = [Diagnostics.Stopwatch]::StartNew()
    while ($session.IsBusy -or $session.IsStale) {
        if ($timeout.Elapsed.TotalSeconds -gt 30) { throw 'Automatic refresh did not complete.' }
        Pump-Dispatcher
    }
    Assert-True ($session.Results[0].Rows[0]['year'] -eq '2003') 'Debounced automatic refresh uses the latest datasource.'

    $session.AutoRefresh = $false
    $definition.Sql = 'SELECT SUM(i) AS total FROM range(50000000) t(i)'
    $file.NotifyMetadataChanged()
    $task = $session.RefreshAsync()
    $session.IsJsonEnabled = $false
    Wait-UiTask $task
    Assert-True ($session.Results.Count -eq 1 -and $session.CanRefresh) 'Turning JSON off retains valid in-flight query results.'
    Wait-UiTask ($session.RefreshAsync())
    Assert-True ($session.Results.Count -eq 1 -and [string]::IsNullOrWhiteSpace($session.Results[0].Error)) 'Statistics run without a JSON companion in session-only mode.'
    Assert-True ($session.PersistenceWarning.Contains('not saved') -and $session.PersistenceWarning.Contains('File information')) 'Session-only warning explains persistence and points to File information.'
    $session.NewStatistic()
    $session.EditorName = 'Session count'
    $session.EditorSql = 'SELECT COUNT(*) AS total FROM records'
    Wait-UiTask ($session.SaveAndRunAsync())
    Assert-True ($session.Definitions.Count -eq 2 -and $session.Results[1].Rows[0]['total'] -eq 1) 'Users can add and test statistics while JSON persistence is off.'
    Wait-UiTask ($session.DeleteSelectedAsync())
    Assert-True ($session.Definitions.Count -eq 1) 'Users can delete session-only statistics.'

    $catalogType = $assembly.GetType('WatchedFilmsTracker.Source.Statistics.DefaultStatisticsCatalog', $true)
    $emptyTemplate = $catalogType.GetProperty('Empty', [Reflection.BindingFlags]'Static,Public,NonPublic').GetValue($null)
    $selectedSql = $session.EditorSql
    $session.SelectedTemplate = $emptyTemplate
    Assert-True ($session.DraftSql.Contains('Edit me') -and $session.DraftName -eq 'New statistic') 'The Empty template provides an editable SQL starting point.'
    Assert-True ($session.EditorSql -eq $selectedSql) 'Choosing an add template does not overwrite the selected statistic.'
    $session.DraftName = 'Draft count'
    $session.DraftSql = 'SELECT COUNT(*) AS total FROM records'
    Wait-UiTask ($session.AddDraftAsync())
    Assert-True ($session.Definitions.Count -eq 2 -and $session.Results[1].Rows[0]['total'] -eq 1) 'The add panel creates and runs the customized draft.'
    Assert-True ($session.CanMoveUp -and -not $session.CanMoveDown) 'Ordering buttons reflect the selected last statistic.'
    Wait-UiTask ($session.MoveSelectedAsync(-1))
    Assert-True ($session.Definitions[0].Name -eq 'Draft count' -and $session.Results[0].Name -eq 'Draft count') 'Moving up reorders both definitions and result tables.'
    Assert-True (-not $session.CanMoveUp -and $session.CanMoveDown) 'Ordering buttons respect the first-statistic boundary.'
    Wait-UiTask ($session.MoveSelectedAsync(-1))
    Assert-True ($session.Definitions[0].Name -eq 'Draft count') 'Moving beyond the list boundary is ignored safely.'
    Wait-UiTask ($session.MoveSelectedAsync(1))
    Assert-True ($session.Definitions[1].Name -eq 'Draft count') 'Moving down restores the selected statistic to the next position.'
}
finally {
    $session.Dispose()
}

$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('WatchedFilmsTrackerUi-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($testRoot)
$csvPath = Join-Path $testRoot 'collection.csv'
$sidecarPath = [IO.Path]::ChangeExtension($csvPath, '.metadata.json')
try {
    [IO.File]::WriteAllText($csvPath, "Title`tRelease Year`r`nExample`t2020`r`n")
    [IO.File]::WriteAllText($sidecarPath, @'
{
  "collectionType": "Podcasts",
  "statistics": [{ "name": "Total", "sql": "SELECT COUNT(*) AS total FROM records" }],
  "statisticsSettings": { "loadOnOpen": false, "autoRefresh": false }
}
'@)
    $reloadedFile = [WatchedFilmsTracker.Source.ManagingFilmsFile.CollectionFile]::new($csvPath)
    $reloadedSession = New-Session $reloadedFile
    try {
        Wait-UiTask ($reloadedSession.InitializeAsync())
        Assert-True ($reloadedFile.CommonCollectionType.Name -eq 'Podcasts') 'An unknown collection type loads without losing its name.'
        Assert-True ($reloadedSession.Results.Count -eq 0) 'Persisted LoadOnOpen=false is honored.'
        $reloadedSession.AutoRefresh = $true
        $reloadedSession.IsJsonEnabled = $false
        [void]$reloadedFile.RevertChanges()
        Assert-True ($reloadedSession.IsJsonEnabled -and -not $reloadedSession.AutoRefresh) 'Revert restores JSON refresh settings.'
        Assert-True ($reloadedSession.Results.Count -eq 0) 'Revert honors restored LoadOnOpen=false.'

        $reloadedFile.Metadata.StatisticsSettings.LoadOnOpen = $true
        $autoLoadedSession = New-Session $reloadedFile
        try {
            Wait-UiTask ($autoLoadedSession.InitializeAsync())
            Assert-True ($autoLoadedSession.Results.Count -eq 1 -and -not $autoLoadedSession.IsStale) 'LoadOnOpen=true calculates saved queries on first load.'
        }
        finally {
            $autoLoadedSession.Dispose()
        }

        $panel = [WatchedFilmsTracker.Source.Views.StatisticsPanel]::new()
        $panel.ShowCollection($reloadedFile)
        $panel.DataContext.IsJsonEnabled = $false
        Pump-Dispatcher
        $body = $panel.FindName('StatisticsBody')
        $warning = $panel.FindName('SessionOnlyWarning')
        $editor = $panel.FindName('StatisticsEditorExpander')
        Assert-True $body.IsEnabled 'XAML leaves statistics usable with session-only queries.'
        Assert-True ($warning.Visibility -eq [Windows.Visibility]::Visible) 'XAML shows the unsaved-query warning when JSON is off.'
        Assert-True (-not $editor.IsExpanded) 'The add/edit statistics controls are folded by default.'
        Assert-True ($editor.Header -eq 'Modify statistics') 'The statistics editor is labeled Modify statistics.'
        Assert-True ($panel.FindName('StatisticsSettingsPanel') -is [Windows.Controls.GroupBox]) 'Statistics settings are in an always-visible bordered group.'
        Assert-True ($panel.FindName('ModifyStatisticsPanel') -is [Windows.Controls.GroupBox] -and
            $panel.FindName('AddStatisticsPanel') -is [Windows.Controls.GroupBox]) 'Modify and Add new statistics each have a border.'
        $resultsItems = $panel.FindName('StatisticResultsItems')
        Assert-True ([object]::ReferenceEquals($body.Children[0], $resultsItems)) 'Statistic result tables appear first in the panel.'
        $templateView = $resultsItems.ItemTemplate.LoadContent()
        $table = [Data.DataTable]::new()
        $templateView.DataContext = [WatchedFilmsTracker.Source.Statistics.StatisticReportResult]::new('Result', $table.DefaultView)
        Pump-Dispatcher
        Assert-True ($templateView.Content.Children[0].Visibility -eq [Windows.Visibility]::Collapsed) 'Successful result grids have no blank error-message gap above them.'
        Assert-True ($null -eq $panel.FindName('UseJsonCheckBox')) 'The JSON companion checkbox is no longer duplicated in statistics.'
        Assert-True ($panel.DataContext.MetadataPath -eq $sidecarPath) 'The companion path is exposed as raw copyable text.'
        $panel.DataContext.IsJsonEnabled = $true
        Pump-Dispatcher
        Assert-True ($body.IsEnabled -and $warning.Visibility -eq [Windows.Visibility]::Collapsed) 'Enabling JSON hides the session-only persistence warning.'
        $panel.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.FrameworkElement]::UnloadedEvent))

        $orderedDefinition = [WatchedFilmsTracker.Source.Statistics.StatisticDefinition]::new()
        $orderedDefinition.Name = 'First after moving'
        $orderedDefinition.Sql = 'SELECT COUNT(*) AS total FROM records'
        $reloadedFile.Metadata.Statistics.Add($orderedDefinition)
        $reloadedSession.SelectedDefinition = $orderedDefinition
        $reloadedFile.NotifyMetadataChanged()
        Wait-UiTask ($reloadedSession.MoveSelectedAsync(-1))
        [void]$reloadedFile.Save()
        $savedOrder = [IO.File]::ReadAllText($sidecarPath) | ConvertFrom-Json
        Assert-True ($savedOrder.statistics[0].name -eq 'First after moving') 'The selected display order is saved in JSON.'

    }
    finally {
        $reloadedSession.Dispose()
    }
}
finally {
    $resolvedRoot = [IO.Path]::GetFullPath($testRoot)
    $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (-not $resolvedRoot.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing cleanup outside the system temporary directory.'
    }
    foreach ($path in @($csvPath, $sidecarPath)) {
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    Remove-Item -LiteralPath $resolvedRoot
}
