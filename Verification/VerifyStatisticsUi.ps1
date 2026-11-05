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

function Wait-StatisticsIdle($Session) {
    $timeout = [Diagnostics.Stopwatch]::StartNew()
    do {
        Pump-Dispatcher
        if ($timeout.Elapsed.TotalSeconds -gt 30) { throw 'Statistics action timed out in verification.' }
    } while ($Session.IsBusy)
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
    Assert-True ($session.RecentlyDeletedStatistics.Count -eq 1 -and $session.HasDeletedStatistics) 'Deleted statistics remain recoverable while JSON is off.'
    $deleted = $session.RecentlyDeletedStatistics[0]
    Assert-True ($deleted.DisplayText -eq 'Deleted today - Session count') 'Recently deleted statistics show a readable age and statistic name.'
    $session.SelectedDeletedStatistic = $deleted
    Wait-UiTask ($session.RestoreSelectedAsync())
    Assert-True ($session.Definitions.Count -eq 2 -and $session.Definitions[1].Name -eq 'Session count' -and
        $session.RecentlyDeletedStatistics.Count -eq 0) 'Restoring returns the original statistic and removes its history entry.'
    Wait-UiTask ($session.DeleteSelectedAsync())

    $deleted = $session.RecentlyDeletedStatistics[0]
    $deleted.DeletedAt = [DateTimeOffset]::new([DateTime]::Today.AddDays(-1).AddHours(12))
    Assert-True ($deleted.DisplayText -eq 'Deleted 1 day ago - Session count') 'A statistic deleted yesterday uses the singular day label.'
    $deleted.DeletedAt = [DateTimeOffset]::new([DateTime]::Today.AddDays(-5).AddHours(12))
    Assert-True ($deleted.DisplayText -eq 'Deleted 5 days ago - Session count') 'Older deleted statistics show the number of calendar days.'

    $catalogType = $assembly.GetType('WatchedFilmsTracker.Source.Statistics.DefaultStatisticsCatalog', $true)
    $emptyTemplate = $catalogType.GetProperty('Empty', [Reflection.BindingFlags]'Static,Public,NonPublic').GetValue($null)
    $previousDefinition = $session.SelectedDefinition
    Wait-UiTask ($session.AddDefaultAsync($emptyTemplate))
    Assert-True ($session.EditorSql.Contains('Edit me') -and $session.EditorName -eq 'New statistic') 'The Empty template creates a selected editable statistic.'
    $session.EditorName = 'Draft count'
    $session.EditorSql = 'SELECT COUNT(*) AS total FROM records'
    Assert-True ($session.SelectedDefinition.Name -eq 'Draft count' -and $session.SelectedDefinition.Sql.Contains('COUNT(*)')) 'Editing the shared name and SQL properties updates the selected definition immediately.'
    Assert-True ($emptyTemplate.Name -eq 'Empty' -and $emptyTemplate.Sql.Contains('Edit me')) 'Editing a new statistic preserves its original catalog template.'
    $editedDefinition = $session.SelectedDefinition
    $session.SelectedDefinition = $previousDefinition
    $session.SelectedDefinition = $editedDefinition
    Assert-True ($session.EditorName -eq 'Draft count' -and $session.EditorSql.Contains('COUNT(*)')) 'Switching selections preserves edits without applying them.'
    Wait-UiTask ($session.RefreshAsync())
    Assert-True ($session.Definitions.Count -eq 2 -and $session.Results[1].Rows[0]['total'] -eq 1) 'Manual refresh runs the shared editor query.'
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
        $testWindow = [Windows.Window]::new()
        $testWindow.Content = $panel
        $testWindow.Width = 340
        $testWindow.Height = 900
        $testWindow.Opacity = 0
        $testWindow.ShowInTaskbar = $false
        $testWindow.ShowActivated = $false
        $testWindow.Show()
        $panel.ShowCollection($reloadedFile)
        $panel.DataContext.IsJsonEnabled = $false
        Pump-Dispatcher
        $body = $panel.FindName('StatisticsBody')
        $warning = $panel.FindName('SessionOnlyWarning')
        $editor = $panel.FindName('StatisticsEditorExpander')
        Assert-True $body.IsEnabled 'XAML leaves statistics usable with session-only queries.'
        Assert-True ($warning.Visibility -eq [Windows.Visibility]::Visible) 'XAML shows the unsaved-query warning when JSON is off.'
        Assert-True (-not $editor.IsExpanded) 'The add/edit statistics controls are folded by default.'
        $editor.IsExpanded = $true
        Assert-True ($editor.Header -eq 'Modify statistics') 'The statistics editor is labeled Modify statistics.'
        Assert-True ($panel.FindName('StatisticsSettingsPanel') -is [Windows.Controls.GroupBox]) 'Statistics settings are in an always-visible bordered group.'
        Assert-True ($panel.FindName('ModifyStatisticsPanel') -is [Windows.Controls.GroupBox] -and
            $null -eq $panel.FindName('AddStatisticsPanel')) 'One statistics editor replaces the separate add panel.'
        $resultsPanel = $panel.FindName('StatisticsResultsPanel')
        $refreshPanel = $panel.FindName('StatisticsRefreshPanel')
        $refreshButton = $panel.FindName('RefreshStatisticsButton')
        Assert-True ($resultsPanel -is [Windows.Controls.GroupBox] -and $refreshPanel -is [Windows.Controls.GroupBox] -and
            [object]::ReferenceEquals($body.Children[0], $resultsPanel) -and
            [object]::ReferenceEquals($body.Children[1], $refreshPanel) -and
            [object]::ReferenceEquals($body.Children[2], $panel.FindName('StatisticsSettingsPanel'))) 'Results and refresh each have a border, with refresh above settings.'
        $restoreButton = $panel.FindName('RestoreStatisticsButton')
        Assert-True (-not $restoreButton.IsEnabled -and $null -eq $panel.FindName('RecentlyDeletedStatisticsPanel')) 'Restore starts disabled and no history panel occupies the sidebar.'
        Wait-UiTask ($panel.DataContext.DeleteSelectedAsync())
        Pump-Dispatcher
        Assert-True $restoreButton.IsEnabled 'Deleting a statistic enables the restore button.'
        Assert-True (-not $refreshButton.IsEnabled -and $panel.FindName('StatisticsStatusText').Text -eq 'No statistics.' -and
            $panel.FindName('EmptyStatisticsResultsText').Visibility -eq [Windows.Visibility]::Visible -and
            $panel.FindName('EmptyStatisticsResultsText').Text -eq 'No statistics.' -and
            $panel.DataContext.Results.Count -eq 0) 'Deleting the last statistic disables refresh and shows both empty states.'
        $restoreButton.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent))
        Pump-Dispatcher
        $restoreMenu = $restoreButton.ContextMenu
        Assert-True ($restoreMenu.IsOpen -and $restoreMenu.Items.Count -eq 1 -and
            $restoreMenu.Items[0].Header -like 'Deleted today - *') 'Restore opens a popup list with deletion ages and names.'
        $restoreMenu.Items[0].RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.MenuItem]::ClickEvent))
        Wait-StatisticsIdle $panel.DataContext
        Assert-True ($panel.DataContext.Definitions.Count -eq 1 -and -not $panel.DataContext.HasDeletedStatistics -and
            -not $restoreMenu.IsOpen -and $refreshButton.IsEnabled) 'Selecting a popup entry directly restores its query and closes the menu.'
        $addButton = $panel.FindName('AddStatisticButton')
        $addButton.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent))
        Pump-Dispatcher
        $templateMenu = $addButton.ContextMenu
        Assert-True ($templateMenu.IsOpen -and $templateMenu.Items.Count -gt 1 -and
            @($templateMenu.Items | Where-Object Header -eq 'Empty').Count -eq 1) 'Add opens a popup list of built-in templates including Empty.'
        $emptyMenuItem = @($templateMenu.Items | Where-Object Header -eq 'Empty')[0]
        $emptyMenuItem.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.MenuItem]::ClickEvent))
        Wait-StatisticsIdle $panel.DataContext
        Assert-True ($panel.DataContext.Definitions.Count -eq 2 -and $panel.DataContext.EditorName -eq 'New statistic' -and
            -not $templateMenu.IsOpen) 'Choosing a template adds and selects a new statistic directly.'
        $nameBox = $panel.FindName('StatisticNameTextBox')
        $sqlBox = $panel.FindName('StatisticSqlTextBox')
        $nameBox.SetCurrentValue([Windows.Controls.TextBox]::TextProperty, 'Edited directly')
        $sqlBox.SetCurrentValue([Windows.Controls.TextBox]::TextProperty, 'SELECT COUNT(*) AS edited_total FROM records')
        Pump-Dispatcher
        $editedDefinition = $panel.DataContext.SelectedDefinition
        $panel.FindName('ExistingStatisticsList').SelectedItem = $panel.DataContext.Definitions[0]
        Pump-Dispatcher
        $panel.FindName('ExistingStatisticsList').SelectedItem = $editedDefinition
        Pump-Dispatcher
        Assert-True ($nameBox.Text -eq 'Edited directly' -and $sqlBox.Text.Contains('edited_total') -and
            $editedDefinition.Name -eq 'Edited directly' -and $reloadedFile.HasUnsavedChanges) 'The single pair of textboxes updates the selected query and retains edits across selection changes.'
        $panel.DataContext.IsJsonEnabled = $true
        [void]$reloadedFile.Save()
        $editedFile = [WatchedFilmsTracker.Source.ManagingFilmsFile.CollectionFile]::new($csvPath)
        Assert-True ($editedFile.Metadata.Statistics[1].Name -eq 'Edited directly' -and
            $editedFile.Metadata.Statistics[1].Sql.Contains('edited_total')) 'Live editor changes persist without an Apply action.'
        Wait-UiTask ($panel.DataContext.DeleteSelectedAsync())
        $addButton.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent))
        Pump-Dispatcher
        $staleMenu = $addButton.ContextMenu
        $staleItem = $staleMenu.Items[0]
        $otherFile = [WatchedFilmsTracker.Source.ManagingFilmsFile.CollectionFile]::new($type)
        $otherFile.Metadata.StatisticsSettings.LoadOnOpen = $false
        $panel.ShowCollection($otherFile)
        $staleItem.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.MenuItem]::ClickEvent))
        Pump-Dispatcher
        Assert-True (-not $staleMenu.IsOpen -and $otherFile.Metadata.Statistics.Count -eq 0 -and
            $reloadedFile.Metadata.Statistics.Count -eq 1) 'Switching collections closes popup menus and ignores stale menu choices.'
        Assert-True (-not $refreshButton.IsEnabled -and $panel.FindName('EmptyStatisticsResultsText').Text -eq 'No statistics.') 'A new empty collection starts with disabled refresh and an empty result message.'
        $panel.ShowCollection($reloadedFile)
        Pump-Dispatcher
        $resultsItems = $panel.FindName('StatisticResultsItems')
        Assert-True ([object]::ReferenceEquals($resultsItems.Parent.Parent, $resultsPanel)) 'Statistic datagrids are enclosed by the results border.'
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
        $testWindow.Close()

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

        for ($number = 1; $number -le 12; $number++) {
            $reloadedSession.NewStatistic()
            $reloadedSession.EditorName = "Deleted statistic $number"
            $reloadedSession.EditorSql = 'SELECT COUNT(*) AS preserved_total FROM records'
            Wait-UiTask ($reloadedSession.SaveAndRunAsync())
            Wait-UiTask ($reloadedSession.DeleteSelectedAsync())
        }
        Assert-True ($reloadedSession.RecentlyDeletedStatistics.Count -eq 10 -and
            $reloadedSession.RecentlyDeletedStatistics[0].Definition.Name -eq 'Deleted statistic 12' -and
            $reloadedSession.RecentlyDeletedStatistics[9].Definition.Name -eq 'Deleted statistic 3') 'History retains the latest 10 deletions in most-recent-first order.'
        Assert-True $reloadedFile.HasUnsavedChanges 'Deleting statistics marks the companion metadata as unsaved.'
        [void]$reloadedFile.Save()
        $savedHistory = [IO.File]::ReadAllText($sidecarPath) | ConvertFrom-Json
        Assert-True ($savedHistory.recentlyDeletedStatistics.Count -eq 10 -and
            $savedHistory.recentlyDeletedStatistics[0].definition.sql.Contains('preserved_total')) 'JSON saves recent deletion dates and full statistic definitions.'
        $historyFile = [WatchedFilmsTracker.Source.ManagingFilmsFile.CollectionFile]::new($csvPath)
        $historySession = New-Session $historyFile
        try {
            $historySession.SelectedDeletedStatistic = $historySession.RecentlyDeletedStatistics[0]
            $originalId = $historySession.SelectedDeletedStatistic.Definition.Id
            Wait-UiTask ($historySession.RestoreSelectedAsync())
            Assert-True ($historySession.SelectedDefinition.Name -eq 'Deleted statistic 12' -and
                $historySession.SelectedDefinition.Id -eq $originalId -and
                $historySession.RecentlyDeletedStatistics.Count -eq 9 -and $historyFile.HasUnsavedChanges) 'Reloaded history restores the original statistic identity and marks the file for saving.'
            Assert-True ($historySession.Results[2].Rows[0]['preserved_total'] -eq 1) 'A restored query runs against the collection datasource.'
        }
        finally {
            $historySession.Dispose()
        }

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
