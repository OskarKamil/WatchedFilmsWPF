namespace WatchedFilmsTracker.Source.ManagingRecords
{
    public class CellDataType
    {
        public enum DataType
        {
            String,
            Number,
            Date
        }

        public static IEnumerable<DataType> GetValues()
        {
            return (IEnumerable<DataType>)Enum.GetValues(typeof(DataType));
        }
    }
}