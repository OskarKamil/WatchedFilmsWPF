using WatchedFilmsTracker.Source.RecordValueValidator;

namespace WatchedFilmsTracker.Source.DataGridHelpers
{
    public class Cell
    {
        public Column Column { get; set; }
        public bool IsValid { get; set; }
        public string Value { get; set; }

        public Cell(string value, Column column)
        {
            Value = value;
            Column = column;
            Validate();
        }

        public string ToString() { return Value; }

        public Cell(string value)
        {
            Value = value;
            Validate();
        }

        public void UpdateValue(string newValue)
        {
            Value = newValue;
            Validate();
        }

        private void Validate()
        {
           // IsValid = FilmRecordPropertyValidator.Validate(Column.Header, Value);
        }
    }
}