param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\bin\Debug\net8.0-windows7.0\WatchedFilmsTracker.dll')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework, WindowsBase
$assemblyPathResolved = (Resolve-Path -LiteralPath $AssemblyPath).Path
$binPath = Split-Path -Parent $assemblyPathResolved
$env:PATH = (Join-Path $binPath 'runtimes\win-x64\native') + [IO.Path]::PathSeparator + $env:PATH
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPathResolved)
# PowerShell initializes WPF's application resource owner to pwsh before this script runs.
# Override that test-host state so pack URIs resolve against the real application assembly.
[Windows.Application].GetField('_resourceAssembly', [Reflection.BindingFlags]'Static,NonPublic').SetValue($null, $assembly)
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

function Invoke-WindowHandler([string]$Name, [object[]]$Arguments) {
    [void]$window.GetType().GetMethod($Name, [Reflection.BindingFlags]'Instance,Public,NonPublic').Invoke($window, $Arguments)
}

function Invoke-Manager([string]$Name, [object[]]$Arguments) {
    [void]$managerType.GetMethod($Name, [Reflection.BindingFlags]'Static,Public,NonPublic').Invoke($null, $Arguments)
}

function Update-TestLayout([int]$Width = 1250) {
    for ($pass = 0; $pass -lt 3; $pass++) {
        Pump-Dispatcher
        $window.Content.Measure([Windows.Size]::new($Width, 900))
        $window.Content.Arrange([Windows.Rect]::new(0, 0, $Width, 900))
        $window.Content.UpdateLayout()
    }
}

$originalLocation = (Get-Location).Path
$originalDirectory = [Environment]::CurrentDirectory
$testRoot = [string](Join-Path ([IO.Path]::GetTempPath()) ('WatchedFilmsTrackerMainWindow-' + [Guid]::NewGuid().ToString('N')))
[void][IO.Directory]::CreateDirectory($testRoot)
$csvPath = [string](Join-Path $testRoot 'collection.csv')
$jsonPath = [IO.Path]::ChangeExtension($csvPath, '.metadata.json')
$configPath = [string](Join-Path $testRoot 'settings.conf')
$managerType = $assembly.GetType('WatchedFilmsTracker.Source.Managers.OpenedFilesManager', $true)
$window = $null
try {
    Set-Location -LiteralPath $testRoot
    [Environment]::CurrentDirectory = $testRoot
    $window = [WatchedFilmsTracker.MainWindow]::new()
    Pump-Dispatcher
    $programInformationType = $assembly.GetType('WatchedFilmsTracker.Source.ProgramInformation', $true)
    $programNameField = $programInformationType.GetField('PROGRAM_NAME', [Reflection.BindingFlags]'Static,Public,NonPublic')
    $programName = $programNameField.GetValue($null)
    Assert-True ($window.Title -eq $programName) 'Startup without a collection uses the application name as its window title.'

    $search = $window.FindName('searchTextBox')
    $placeholder = $window.FindName('SearchPlaceholderText')
    $clearSearch = $window.FindName('clearSearchButton')
    Assert-True ($search.Text -eq '' -and $placeholder.Visibility -eq [Windows.Visibility]::Visible -and
        $placeholder.Text -eq 'Search collection data' -and -not $placeholder.IsHitTestVisible) 'Search starts empty with a generic, click-through placeholder.'
    Assert-True ($clearSearch.Visibility -eq [Windows.Visibility]::Hidden) 'The clear-search button is hidden when there is no query.'
    $window.Opacity = 0
    $window.ShowActivated = $false
    $window.ShowInTaskbar = $false
    $window.Show()
    [void]$search.Focus()
    Pump-Dispatcher
    Assert-True ($search.IsKeyboardFocusWithin -and $placeholder.Visibility -eq [Windows.Visibility]::Collapsed -and
        $search.Text -eq '') 'The placeholder disappears on the first focus without becoming search text.'
    $search.Text = 'invoice 2026'
    Pump-Dispatcher
    Assert-True ($clearSearch.Visibility -eq [Windows.Visibility]::Visible -and
        $placeholder.Visibility -eq [Windows.Visibility]::Collapsed) 'Typing a query displays the clear button and keeps the placeholder hidden.'
    [void]$window.FindName('ButtonNewFile').Focus()
    Pump-Dispatcher
    Assert-True ($search.Text -eq 'invoice 2026' -and $placeholder.Visibility -eq [Windows.Visibility]::Collapsed) 'Leaving search preserves the real query.'
    Invoke-WindowHandler 'searchClearButton_ClearText' @($clearSearch, [Windows.RoutedEventArgs]::new())
    Pump-Dispatcher
    Assert-True ($search.Text -eq '' -and $search.IsKeyboardFocusWithin -and
        $clearSearch.Visibility -eq [Windows.Visibility]::Hidden -and
        $placeholder.Visibility -eq [Windows.Visibility]::Collapsed) 'Clearing search leaves an empty, focused textbox ready for another query.'
    [void]$window.FindName('ButtonNewFile').Focus()
    Pump-Dispatcher
    Assert-True ($placeholder.Visibility -eq [Windows.Visibility]::Visible -and $search.Text -eq '') 'The generic placeholder returns after an empty search loses focus.'
    $window.Hide()

    foreach ($name in @('buttonAddRecord', 'buttonDeleteFilmRecord', 'buttonClearAll', 'buttonAddColumn',
        'buttoRemoveColumn', 'buttoRenameColumn', 'buttonSave', 'buttonSaveAs', 'buttonRevertChanges')) {
        Assert-True (-not $window.FindName($name).IsEnabled) "No-file startup disables $name."
    }
    foreach ($name in @('ButtonAddColumn_Click', 'ButtonAddRecord_Click', 'ButtonDeleteRecord_Click',
        'RemoveColumnButton_Click', 'RenameColumnButton_Click', 'ClearAll')) {
        Invoke-WindowHandler $name @($window, [Windows.RoutedEventArgs]::new())
    }
    Assert-True $true 'No-file row and column action handlers safely ignore an invocation.'
    Assert-True (-not $window.FindName('FileInformationBody').IsEnabled) 'No-file startup disables collection information controls.'
    Assert-True (-not $window.FindName('TableStructureBody').IsEnabled) 'No-file startup disables table structure controls.'
    $saveAll = $window.FindName('buttonSaveAll')
    $saveAllIcon = $saveAll.Content.Children[0]
    Assert-True (-not $saveAll.IsEnabled -and $saveAllIcon.Opacity -eq 0.5) 'Save All starts disabled with a dimmed icon.'
    Assert-True ($window.FindName('JsonForNewFilesAskRadioButton').IsChecked -eq $true) 'New-file JSON preference defaults to Ask each time.'

    $films = [WatchedFilmsTracker.Source.Models.CommonCollections]::GetCommonCollectionByName('Films')
    Invoke-Manager 'CreateEmptyWorkingFile' @($films)
    # An unshown window does not generate tab containers; select the logical item explicitly.
    $tabs = $managerType.GetProperty('TabsOfOpenedFiles', [Reflection.BindingFlags]'Static,Public,NonPublic').GetValue($null)
    $window.FindName('TabControlMainWindow').SelectedItem = $tabs[$tabs.Count - 1]
    Pump-Dispatcher
    Assert-True ($window.Title.Contains($programName) -and $window.Title.Contains('New File')) 'Creating a collection updates the title while retaining the application name.'
    Assert-True ($window.FindName('buttonAddRecord').IsEnabled -and $window.FindName('buttonAddColumn').IsEnabled) 'An open empty collection enables adding rows and columns.'
    Assert-True (-not $window.FindName('buttonDeleteFilmRecord').IsEnabled -and -not $window.FindName('buttoRemoveColumn').IsEnabled) 'Delete actions remain disabled until cells are selected.'
    Assert-True ($window.FindName('TextBlockCurrentFileCollectionType').Text -eq 'Films') 'The known collection type is displayed correctly.'
    $selectedMenuItems = @($window.FindName('ButtonCurrentFileCollectionType').ContextMenu.Items | Where-Object IsChecked)
    Assert-True ($selectedMenuItems.Count -eq 1 -and $selectedMenuItems[0].Header -eq 'Films') 'The collection-type menu checks the actual known type.'

    $delimiter = $window.FindName('DelimiterComboBox')
    Assert-True ($delimiter -is [Windows.Controls.ComboBox] -and $delimiter.Items.Count -ge 4) 'File information provides a delimiter dropdown.'
    $jsonCheckBox = $window.FindName('UseJsonCompanionCheckBox')
    $jsonPathBox = $window.FindName('TextBoxCompanionPath')
    Assert-True ($jsonCheckBox -is [Windows.Controls.CheckBox]) 'The JSON companion checkbox is in file information.'
    Assert-True ($jsonPathBox -is [Windows.Controls.TextBox] -and $jsonPathBox.IsReadOnly -and $jsonPathBox.Focusable) 'The companion path is selectable and read-only.'
    $newFile = $window.CurrentWorkingFile()
    $jsonCheckBox.SetCurrentValue([Windows.Controls.CheckBox]::IsCheckedProperty, $true)
    Invoke-WindowHandler 'UseJsonCompanionCheckBox_Click' @($jsonCheckBox, [Windows.RoutedEventArgs]::new())
    Pump-Dispatcher
    Assert-True ($newFile.Filepath -eq '' -and $newFile.Metadata.StoreInJson -and $newFile.HasUnsavedChanges) 'Enabling JSON on an unsaved collection remembers the choice without requesting a path.'
    Assert-True ($saveAll.IsEnabled -and $saveAllIcon.Opacity -eq 1.0) 'Save All enables and restores its icon for pending changes.'
    [void]$newFile.RevertChanges()
    Pump-Dispatcher

    foreach ($name in @('FileInformationExpander', 'SettingsExpander', 'TableStructureExpander', 'CollectionStatisticsExpander')) {
        $window.FindName($name).IsExpanded = $true
    }
    $window.FindName('CollectionStatisticsPanel').FindName('StatisticsEditorExpander').IsExpanded = $true
    $pathBoxes = @($window.FindName('TextBoxFilePath'), $jsonPathBox)
    $longPath = 'C:\A folder with spaces\' + ('verylongpathsegment' * 20) + '\My films.csv'
    foreach ($box in $pathBoxes) { $box.SetCurrentValue([Windows.Controls.TextBox]::TextProperty, $longPath) }
    $sidebar = $window.FindName('RightSidebar')
    foreach ($width in @(1150, 1600)) {
        Update-TestLayout $width
        Assert-True ([Math]::Abs($sidebar.ActualWidth - 340) -lt 0.1) "The sidebar stays 340 px wide in a $width px window with all panels expanded."
        Assert-True ($sidebar.ExtentWidth -le $sidebar.ViewportWidth + 1) 'Expanded content cannot increase the sidebar width.'
        foreach ($box in $pathBoxes) {
            Assert-True ($box.LineCount -gt 2 -and $box.ActualWidth -le $sidebar.ViewportWidth) 'Long paths wrap within the available sidebar width.'
            Assert-True ($box.Parent.ActualWidth - $box.ActualWidth -lt 20) 'Path textboxes fill their bordered container.'
        }
    }
    Assert-True ([object]::ReferenceEquals($delimiter.Parent, $window.FindName('ButtonCurrentFileCollectionType').Parent)) 'Delimiter and collection type share one row.'
    $jsonPathBox.GetBindingExpression([Windows.Controls.TextBox]::TextProperty).UpdateTarget()
    Invoke-WindowHandler 'UpdateFileInformation' @()

    [IO.File]::WriteAllText($csvPath, "Title`tRelease Year`r`nExample`t2020`r`n")
    [IO.File]::WriteAllText($jsonPath, '{"collectionType":"Podcasts","statisticsSettings":{"loadOnOpen":false}}')
    Invoke-Manager 'CreateNewWorkingFile' @($csvPath)
    $window.FindName('TabControlMainWindow').SelectedItem = $tabs[$tabs.Count - 1]
    Pump-Dispatcher
    Assert-True ($window.FindName('TextBlockCurrentFileCollectionType').Text -eq 'Podcasts') 'Unknown collection types are displayed by their stored name.'
    $selectedMenuItems = @($window.FindName('ButtonCurrentFileCollectionType').ContextMenu.Items | Where-Object IsChecked)
    Assert-True ($selectedMenuItems.Count -eq 0) 'An unknown collection type is not mislabeled as a known type.'
    Assert-True ($jsonPathBox.Text -eq $jsonPath) 'File information shows the exact copyable companion path.'
    Assert-True ($jsonPathBox.Parent -is [Windows.Controls.GroupBox] -and $jsonPathBox.BorderThickness.Left -gt 0) 'The JSON path uses the same bordered read-only textbox style as the file path.'
    $newFile.Delimiter = ','
    Pump-Dispatcher
    Assert-True ($saveAll.IsEnabled -and $saveAllIcon.Opacity -eq 1.0) 'Changes in a background collection enable Save All and its icon.'
    [void]$newFile.RevertChanges()
    Pump-Dispatcher
    Assert-True (-not $saveAll.IsEnabled -and $saveAllIcon.Opacity -eq 0.5) 'Clearing background changes disables Save All and dims its icon.'

    $currentFile = $window.CurrentWorkingFile()
    $currentFile.SetCompanionStorage($false, $true)
    $csvBefore = [Convert]::ToBase64String([IO.File]::ReadAllBytes($csvPath))
    $jsonCheckBox.SetCurrentValue([Windows.Controls.CheckBox]::IsCheckedProperty, $true)
    Invoke-WindowHandler 'UseJsonCompanionCheckBox_Click' @($jsonCheckBox, [Windows.RoutedEventArgs]::new())
    Pump-Dispatcher
    Assert-True ([IO.File]::Exists($jsonPath) -and $currentFile.Metadata.StoreInJson) 'Ticking the JSON checkbox creates its companion immediately.'
    Assert-True ([Convert]::ToBase64String([IO.File]::ReadAllBytes($csvPath)) -eq $csvBefore) 'The checkbox creates metadata without rewriting record data.'
    # With no companion present, disabling needs no delete prompt.
    [IO.File]::Delete($jsonPath)
    $jsonCheckBox.SetCurrentValue([Windows.Controls.CheckBox]::IsCheckedProperty, $false)
    Invoke-WindowHandler 'UseJsonCompanionCheckBox_Click' @($jsonCheckBox, [Windows.RoutedEventArgs]::new())
    Pump-Dispatcher
    Assert-True (-not $currentFile.Metadata.StoreInJson -and $jsonCheckBox.IsChecked -eq $false) 'The checkbox and persistence state remain synchronized after disabling.'

    $yesRadio = $window.FindName('JsonForNewFilesYesRadioButton')
    $yesRadio.IsChecked = $true
    Invoke-WindowHandler 'JsonForNewFilesPreference_Click' @($yesRadio, [Windows.RoutedEventArgs]::new())
    Assert-True ([WatchedFilmsTracker.Source.Managers.SettingsManager]::JsonForNewFiles.ToString() -eq 'Yes') 'The Yes radio updates the new-file JSON preference.'

    $expectedStates = @{
        FileInformationExpander = $true
        SettingsExpander = $false
        TableStructureExpander = $false
        CollectionStatisticsExpander = $true
    }
    foreach ($name in $expectedStates.Keys) { $window.FindName($name).IsExpanded = $expectedStates[$name] }
    Pump-Dispatcher
    # All collections are clean, so this exercises close-time settings persistence without dialogs.
    Invoke-WindowHandler 'MainWindow_Closing' @($window, [ComponentModel.CancelEventArgs]::new())
    Assert-True ([IO.File]::Exists($configPath)) 'Closing the window saves program settings.'
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadDefaultSettings()
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadSettingsFromConfigFile()
    Assert-True ([WatchedFilmsTracker.Source.Managers.SettingsManager]::JsonForNewFiles.ToString() -eq 'Yes') 'The new-file JSON preference survives saving and reloading settings.'
    Assert-True ([WatchedFilmsTracker.Source.Managers.SettingsManager]::FileInformationExpanded -and
        -not [WatchedFilmsTracker.Source.Managers.SettingsManager]::SettingsExpanded -and
        -not [WatchedFilmsTracker.Source.Managers.SettingsManager]::TableStructureExpanded -and
        [WatchedFilmsTracker.Source.Managers.SettingsManager]::CollectionStatisticsExpanded) 'Expanded-panel preferences survive saving and reloading program settings.'
    Invoke-WindowHandler 'ApplyUserSettingsToTheProgram' @()
    foreach ($name in $expectedStates.Keys) {
        Assert-True ($window.FindName($name).IsExpanded -eq $expectedStates[$name]) "Reloaded settings restore $name."
    }
}
finally {
    if ($window -ne $null) {
        $panel = $window.FindName('CollectionStatisticsPanel')
        if ($panel -ne $null) { $panel.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.FrameworkElement]::UnloadedEvent)) }
    }
    Set-Location -LiteralPath $originalLocation
    [Environment]::CurrentDirectory = $originalDirectory
    $resolvedRoot = [IO.Path]::GetFullPath($testRoot)
    if (-not $resolvedRoot.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing cleanup outside the system temporary directory.'
    }
    foreach ($path in @($csvPath, $jsonPath, $configPath)) {
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    Remove-Item -LiteralPath $resolvedRoot
}
