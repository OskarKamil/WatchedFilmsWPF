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

    $currentFile = $window.CurrentWorkingFile()
    $currentFile.SetCompanionStorage($false, $true)
    $csvBefore = [Convert]::ToBase64String([IO.File]::ReadAllBytes($csvPath))
    $jsonCheckBox.IsChecked = $true
    Invoke-WindowHandler 'UseJsonCompanionCheckBox_Click' @($jsonCheckBox, [Windows.RoutedEventArgs]::new())
    Pump-Dispatcher
    Assert-True ([IO.File]::Exists($jsonPath) -and $currentFile.Metadata.StoreInJson) 'Ticking the JSON checkbox creates its companion immediately.'
    Assert-True ([Convert]::ToBase64String([IO.File]::ReadAllBytes($csvPath)) -eq $csvBefore) 'The checkbox creates metadata without rewriting record data.'
    # With no companion present, disabling needs no delete prompt.
    [IO.File]::Delete($jsonPath)
    $jsonCheckBox.IsChecked = $false
    Invoke-WindowHandler 'UseJsonCompanionCheckBox_Click' @($jsonCheckBox, [Windows.RoutedEventArgs]::new())
    Pump-Dispatcher
    Assert-True (-not $currentFile.Metadata.StoreInJson -and $jsonCheckBox.IsChecked -eq $false) 'The checkbox and persistence state remain synchronized after disabling.'

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
