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

        /// <summary>
        /// This is required for populating the ComboBox in the UI with the available data types for each cell.
        /// This is the only way that GUI can access all possible enum values at runtime.
        /// </summary>
        ///
        /// <returns>
        /// Returns a list of all possible values of the DataType enum.
        /// </returns>
        public static IEnumerable<DataType> GetValues()
        {
            return (IEnumerable<DataType>)Enum.GetValues(typeof(DataType));
        }
    }
}