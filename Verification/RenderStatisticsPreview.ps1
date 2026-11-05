param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\bin\Debug\net8.0-windows7.0\WatchedFilmsTracker.dll'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts\previews')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework, WindowsBase
$assemblyPathResolved = (Resolve-Path -LiteralPath $AssemblyPath).Path
$binPath = Split-Path -Parent $assemblyPathResolved
$env:PATH = (Join-Path $binPath 'runtimes\win-x64\native') + [IO.Path]::PathSeparator + $env:PATH
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPathResolved)
# PowerShell owns the default WPF resource assembly; use the app's pack resources for this test host.
[Windows.Application].GetField('_resourceAssembly', [Reflection.BindingFlags]'Static,NonPublic').SetValue($null, $assembly)
[Threading.SynchronizationContext]::SetSynchronizationContext(
    [Windows.Threading.DispatcherSynchronizationContext]::new([Windows.Threading.Dispatcher]::CurrentDispatcher))

function Pump-Dispatcher {
    $frame = [Windows.Threading.DispatcherFrame]::new()
    $timer = [Windows.Threading.DispatcherTimer]::new()
    $timer.Interval = [TimeSpan]::FromMilliseconds(30)
    $timer.Add_Tick({ $timer.Stop(); $frame.Continue = $false }.GetNewClosure())
    $timer.Start()
    [Windows.Threading.Dispatcher]::PushFrame($frame)
}

function Save-Visual([Windows.FrameworkElement]$Visual, [int]$Width, [string]$Path) {
    # Expander templates/data-bound children may settle after the first offscreen layout pass.
    for ($pass = 0; $pass -lt 3; $pass++) {
        Pump-Dispatcher
        $Visual.InvalidateMeasure()
        $Visual.Measure([Windows.Size]::new($Width, [double]::PositiveInfinity))
        $height = [int][Math]::Ceiling($Visual.DesiredSize.Height)
        $Visual.Arrange([Windows.Rect]::new(0, 0, $Width, $height))
        $Visual.UpdateLayout()
    }
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new(
        $Width, $height, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
    $background = [Windows.Media.DrawingVisual]::new()
    $context = $background.RenderOpen()
    $context.DrawRectangle([Windows.Media.Brushes]::White, $null, [Windows.Rect]::new(0, 0, $Width, $height))
    $context.Close()
    $bitmap.Render($background)
    $bitmap.Render($Visual)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [IO.File]::Create($Path)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
    Write-Output "$Path ($Width x $height)"
}

function Get-VisualDescendants([Windows.DependencyObject]$Visual) {
    for ($index = 0; $index -lt [Windows.Media.VisualTreeHelper]::GetChildrenCount($Visual); $index++) {
        $child = [Windows.Media.VisualTreeHelper]::GetChild($Visual, $index)
        Write-Output $child
        Get-VisualDescendants $child
    }
}

[void][IO.Directory]::CreateDirectory($OutputDirectory)
[WatchedFilmsTracker.Source.Managers.SettingsManager]::LoadDefaultSettings()
$films = [WatchedFilmsTracker.Source.Models.CommonCollections]::GetCommonCollectionByName('Films')
$file = [WatchedFilmsTracker.Source.ManagingFilmsFile.CollectionFile]::new($films)
$file.Metadata.StatisticsSettings.LoadOnOpen = $false
$file.Metadata.StoreInJson = $true
foreach ($name in @('Count by release year', 'Count by watched year', 'Average rating')) {
    $definition = [WatchedFilmsTracker.Source.Statistics.StatisticDefinition]::new()
    $definition.Name = $name
    $definition.Sql = 'SELECT COUNT(*) AS total FROM records'
    $file.Metadata.Statistics.Add($definition)
}
$panel = [WatchedFilmsTracker.Source.Views.StatisticsPanel]::new()
$panel.ShowCollection($file)
$panel.FindName('StatisticsEditorExpander').IsExpanded = $true
$table = [Data.DataTable]::new()
[void]$table.Columns.Add('Release year', [int])
[void]$table.Columns.Add('Count', [int])
[void]$table.Rows.Add(1994, 12)
[void]$table.Rows.Add(2001, 8)
[void]$table.Rows.Add(2024, 17)
$panel.DataContext.Results.Add([WatchedFilmsTracker.Source.Statistics.StatisticReportResult]::new(
    'Count by release year', $table.DefaultView))
$border = [Windows.Controls.Border]::new()
$border.Background = [Windows.Media.Brushes]::White
$border.Padding = [Windows.Thickness]::new(8)
$border.Child = $panel
try {
    Save-Visual $border 416 (Join-Path $OutputDirectory 'statistics-panel-preview.png')
    $errorText = Get-VisualDescendants $panel | Where-Object { $_ -is [Windows.Controls.TextBlock] -and $_.Name -eq 'StatisticErrorText' } | Select-Object -First 1
    if ($null -eq $errorText -or $errorText.Visibility -ne [Windows.Visibility]::Collapsed) {
        throw 'Successful result tables must collapse the error text to remove its top gap.'
    }
    Write-Output 'PASS: Successful result tables collapse the empty error text above the grid.'
    $panel.FindName('StatisticsEditorExpander').IsExpanded = $false
    Save-Visual $border 416 (Join-Path $OutputDirectory 'statistics-panel-folded-preview.png')
}
finally {
    $panel.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.FrameworkElement]::UnloadedEvent))
}

$originalLocation = (Get-Location).Path
$originalDirectory = [Environment]::CurrentDirectory
$testRoot = [string](Join-Path ([IO.Path]::GetTempPath()) ('WatchedFilmsTrackerPreview-' + [Guid]::NewGuid().ToString('N')))
[void][IO.Directory]::CreateDirectory($testRoot)
$csvPath = [string](Join-Path $testRoot 'My films.csv')
$jsonPath = [IO.Path]::ChangeExtension($csvPath, '.metadata.json')
$window = $null
try {
    [IO.File]::WriteAllText($csvPath, "Title`tRelease Year`r`nExample`t2024`r`n")
    [IO.File]::WriteAllText($jsonPath, '{"collectionType":"Films","statisticsSettings":{"loadOnOpen":false}}')
    Set-Location -LiteralPath $testRoot
    [Environment]::CurrentDirectory = $testRoot
    $window = [WatchedFilmsTracker.MainWindow]::new()
    $managerType = $assembly.GetType('WatchedFilmsTracker.Source.Managers.OpenedFilesManager', $true)
    $arguments = [object[]]::new(1); $arguments[0] = $csvPath
    [void]$managerType.GetMethod('CreateNewWorkingFile', [Reflection.BindingFlags]'Static,Public,NonPublic').Invoke($null, $arguments)
    $tabs = $managerType.GetProperty('TabsOfOpenedFiles', [Reflection.BindingFlags]'Static,Public,NonPublic').GetValue($null)
    $window.FindName('TabControlMainWindow').SelectedItem = $tabs[$tabs.Count - 1]
    $information = $window.FindName('FileInformationExpander')
    $information.IsExpanded = $true
    Pump-Dispatcher
    Save-Visual $information 400 (Join-Path $OutputDirectory 'file-information-preview.png')
}
finally {
    if ($window -ne $null) {
        $window.FindName('CollectionStatisticsPanel').RaiseEvent(
            [Windows.RoutedEventArgs]::new([Windows.FrameworkElement]::UnloadedEvent))
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
