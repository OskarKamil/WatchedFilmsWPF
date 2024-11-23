using System.ComponentModel;

namespace WatchedFilmsTracker.Source.ManagingRecords
{
    public enum DataType
    {
        String,
        Number,
        Date
    }

    public class Cell : INotifyPropertyChanged
    {
        public DataType CellDataType { get; set; }

        public object ComparableValue
        {
            get
            {
                return CellDataType switch
                {
                    DataType.Number => int.TryParse(Value, out var number) ? number : Value,
                    DataType.Date => DateTime.TryParseExact(Value, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var date) ? date : Value,
                    _ => Value
                };
            }
        }

        public bool IsValid
        {
            get => _isValid;
            private set
            {
                if (_isValid != value)
                {
                    _isValid = value;
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                    Validate();
                }
            }
        }

        private bool _isValid;
        private string _value;

        public Cell(string value)
        {
            CellDataType = DataType.String;
            Value = value;
            Validate();
        }

        public Cell(string value, DataType datatype)
        {
            CellDataType = datatype;
            Value = value;
            Validate();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Validate()
        {
            // IsValid = FilmRecordPropertyValidator.Validate(Column.Header, Value);
        }
    }
}