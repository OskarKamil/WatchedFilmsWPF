using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WatchedFilmsTracker.Source.Views;

/// <summary>Builds the record editor view without performing collection or file operations.</summary>
internal static class CollectionGridFactory
{
    public static (Grid Container, DataGrid RecordsGrid) Create()
    {
        Grid container = new() { Name = "grid" };
        DataGrid recordsGrid = new()
        {
            Name = "dataGrid",
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            CanUserResizeRows = false,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            ColumnWidth = DataGridLength.Auto,
            AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(0xD6, 0xE2, 0xFF)),
            AlternationCount = 4,
            SelectionUnit = DataGridSelectionUnit.CellOrRowHeader,
            HorizontalAlignment = HorizontalAlignment.Left,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
        };
        ScrollViewer.SetCanContentScroll(recordsGrid, true);
        ScrollViewer.SetVerticalScrollBarVisibility(recordsGrid, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(recordsGrid, ScrollBarVisibility.Auto);
        container.Children.Add(recordsGrid);
        return (container, recordsGrid);
    }
}
