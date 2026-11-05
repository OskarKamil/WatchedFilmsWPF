param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\bin\Debug\net8.0-windows7.0\WatchedFilmsTracker.dll')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework, WindowsBase
$assemblyPathResolved = (Resolve-Path -LiteralPath $AssemblyPath).Path
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPathResolved)
[Windows.Application].GetField('_resourceAssembly', [Reflection.BindingFlags]'Static,NonPublic').SetValue($null, $assembly)
[Threading.SynchronizationContext]::SetSynchronizationContext(
    [Windows.Threading.DispatcherSynchronizationContext]::new([Windows.Threading.Dispatcher]::CurrentDispatcher))

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    Write-Output "PASS: $Message"
}

function Invoke-WindowHandler([string]$Name, [object[]]$Arguments) {
    [void]$window.GetType().GetMethod($Name, [Reflection.BindingFlags]'Instance,Public,NonPublic').Invoke($window, $Arguments)
}

# Drive the real modal loop on its WPF dispatcher, without requiring manual input.
function Invoke-DialogChoice([string]$ButtonName, [scriptblock]$Action) {
    $state = @{ Chosen = $false; PrimaryLabel = ''; SecondaryLabel = '' }
    $timer = [Windows.Threading.DispatcherTimer]::new()
    $timer.Interval = [TimeSpan]::FromMilliseconds(20)
    $timer.Add_Tick({
        $dialog = @($window.OwnedWindows | Where-Object { $_ -is [WatchedFilmsTracker.Source.Views.JsonCompanionDialog] })[0]
        if ($null -ne $dialog) {
            $timer.Stop()
            $dialog.Opacity = 0
            $state.PrimaryLabel = $dialog.FindName('PrimaryButton').Content
            $state.SecondaryLabel = $dialog.FindName('SecondaryButton').Content
            $state.Chosen = $true
            if ($ButtonName -eq 'Cancel') { $dialog.Close() }
            else { $dialog.FindName($ButtonName).RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Button]::ClickEvent)) }
        }
    }.GetNewClosure())
    $timer.Start()
    try { & $Action }
    finally { $timer.Stop() }
    Assert-True $state.Chosen 'The requested action displayed the JSON choice dialog.'
    return $state
}

$originalLocation = (Get-Location).Path
$originalDirectory = [Environment]::CurrentDirectory
$testRoot = [string](Join-Path ([IO.Path]::GetTempPath()) ('WatchedFilmsTrackerJsonDialogs-' + [Guid]::NewGuid().ToString('N')))
[void][IO.Directory]::CreateDirectory($testRoot)
$csvPath = [string](Join-Path $testRoot 'new collection.csv')
$jsonPath = [IO.Path]::ChangeExtension($csvPath, '.metadata.json')
$window = $null
try {
    Set-Location -LiteralPath $testRoot
    [Environment]::CurrentDirectory = $testRoot
    $window = [WatchedFilmsTracker.MainWindow]::new()
    $window.Opacity = 0
    $window.ShowActivated = $false
    $window.ShowInTaskbar = $false
    $window.Show()
    $newMenuItem = $window.FindName('ButtonNewFile').ContextMenu.Items[0]
    $newAction = { $newMenuItem.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.MenuItem]::ClickEvent)) }

    $choice = Invoke-DialogChoice 'PrimaryButton' $newAction
    $file = $window.CurrentWorkingFile()
    Assert-True ($choice.PrimaryLabel -eq 'Create JSON when saving' -and $choice.SecondaryLabel -eq 'Use CSV only') 'New-file choices explicitly describe when JSON is created.'
    Assert-True ($file.Metadata.StoreInJson -and $file.Filepath -eq '' -and @(Get-ChildItem -LiteralPath $testRoot).Count -eq 0) 'Choosing JSON for a new collection remembers the decision without creating either file.'
    $file.SaveFileAtLocation($csvPath)
    Assert-True ([IO.File]::Exists($csvPath) -and [IO.File]::Exists($jsonPath)) 'Saving a collection after the prompt creates its CSV and JSON together.'

    $jsonCheckBox = $window.FindName('UseJsonCompanionCheckBox')
    $disableAction = {
        $jsonCheckBox.SetCurrentValue([Windows.Controls.CheckBox]::IsCheckedProperty, $false)
        Invoke-WindowHandler 'UseJsonCompanionCheckBox_Click' @($jsonCheckBox, [Windows.RoutedEventArgs]::new())
    }
    $choice = Invoke-DialogChoice 'Cancel' $disableAction
    Assert-True ($file.Metadata.StoreInJson -and $jsonCheckBox.IsChecked -eq $true -and [IO.File]::Exists($jsonPath)) 'Closing the deletion dialog cancels disabling JSON and restores the checkbox.'
    Assert-True ($choice.PrimaryLabel -eq 'Delete JSON file' -and $choice.SecondaryLabel -eq 'Keep JSON file') 'Deletion choices explicitly name the file action.'
    $jsonBefore = [IO.File]::ReadAllText($jsonPath)
    $null = Invoke-DialogChoice 'SecondaryButton' $disableAction
    Assert-True (-not $file.Metadata.StoreInJson -and [IO.File]::ReadAllText($jsonPath) -eq $jsonBefore) 'Keep JSON file disables future JSON saves and preserves the existing file.'
    $file.SetCompanionStorage($true)
    $null = Invoke-DialogChoice 'PrimaryButton' $disableAction
    Assert-True (-not $file.Metadata.StoreInJson -and -not [IO.File]::Exists($jsonPath) -and [IO.File]::Exists($csvPath)) 'Delete JSON file disables JSON and removes only the companion.'

    $null = Invoke-DialogChoice 'SecondaryButton' $newAction
    $csvOnly = $window.CurrentWorkingFile()
    Assert-True (-not $csvOnly.Metadata.StoreInJson -and $csvOnly.Filepath -eq '') 'Use CSV only creates a new collection without pending JSON.'
    $tabCount = $window.FindName('TabControlMainWindow').Items.Count
    $null = Invoke-DialogChoice 'Cancel' $newAction
    Assert-True ($window.FindName('TabControlMainWindow').Items.Count -eq $tabCount -and $window.CurrentWorkingFile() -eq $csvOnly) 'Canceling the new-file prompt leaves the current collection and tab count unchanged.'

    [WatchedFilmsTracker.Source.Managers.SettingsManager]::JsonForNewFiles = 'Yes'
    & $newAction
    $yesFile = $window.CurrentWorkingFile()
    Assert-True ($window.FindName('TabControlMainWindow').Items.Count -eq $tabCount + 1 -and $yesFile.Metadata.StoreInJson -and $yesFile.HasUnsavedChanges -and $window.FindName('buttonSave').IsEnabled) 'The Yes preference creates a new collection without prompting and enables Save for pending JSON.'
    [WatchedFilmsTracker.Source.Managers.SettingsManager]::JsonForNewFiles = 'No'
    & $newAction
    Assert-True ($window.FindName('TabControlMainWindow').Items.Count -eq $tabCount + 2 -and -not $window.CurrentWorkingFile().Metadata.StoreInJson) 'The No preference creates a CSV-only collection without prompting.'
}
finally {
    if ($null -ne $window) {
        $window.FindName('CollectionStatisticsPanel').RaiseEvent([Windows.RoutedEventArgs]::new([Windows.FrameworkElement]::UnloadedEvent))
        $window.Hide()
    }
    Set-Location -LiteralPath $originalLocation
    [Environment]::CurrentDirectory = $originalDirectory
    $resolvedRoot = [IO.Path]::GetFullPath($testRoot)
    if (-not $resolvedRoot.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing cleanup outside the system temporary directory.'
    }
    foreach ($path in @($csvPath, $jsonPath)) {
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    Remove-Item -LiteralPath $resolvedRoot
}
