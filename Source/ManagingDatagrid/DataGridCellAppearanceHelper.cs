using System.Windows.Controls;
using System.Windows.Media;

namespace WatchedFilmsTracker.Source.DataGridHelpers
{
    internal class DataGridCellAppearanceHelper
    {
        public static void MakeCellAppearInvalid(DataGridCell cell, bool makeInvalid)
        {
            if (cell != null)
            {
                if (makeInvalid)
                {
                    cell.Background = Brushes.Red;
                }
                else
                {
                    cell.ClearValue(DataGridCell.BackgroundProperty);
                };
            }
        }
    }
}