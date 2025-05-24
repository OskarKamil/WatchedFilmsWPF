using System.Globalization;
using System.Windows.Data;
using WatchedFilmsTracker.Source.ManagingRecords;

namespace WatchedFilmsTracker.Source.Converters
{
    public class DataTypeComparisonConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2 || values[0] == null || values[1] == null)
                return false;

            if (values[0] is CellDataType.DataType columnDataType && values[1] is CellDataType.DataType radioButtonType)
            {
                return columnDataType == radioButtonType;
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}