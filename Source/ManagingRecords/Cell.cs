using System.ComponentModel;
using static WatchedFilmsTracker.Source.ManagingRecords.CellDataType;

namespace WatchedFilmsTracker.Source.ManagingRecords
{
    public class Cell : INotifyPropertyChanged
    {
        public object ComparableValue
        {
            get
            {
                return DataType switch
                {
                    DataType.Number => int.TryParse(Value, out var number) ? number : Value,
                    DataType.Date => DateTime.TryParseExact(Value, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var date) ? date : Value,
                    _ => Value
                };
            }
        }

        public DataType DataType { get; set; }

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
            DataType = DataType.String;
            Value = value;
            Validate();
        }

        public Cell(string value, DataType datatype)
        {
            DataType = datatype;
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