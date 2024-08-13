using System.ComponentModel;

namespace WatchedFilmsTracker.Source.DataGridHelpers
{
    public class Cell : INotifyPropertyChanged
    {
        public Column Column { get; set; }

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
            Value = value;
            Validate();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString() => Value;

        public void UpdateValue(string newValue)
        {
            Value = newValue; // This will automatically trigger OnPropertyChanged via the Value setter
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Validate()
        {
            // IsValid = FilmRecordPropertyValidator.Validate(Column.Header, Value);
        }
    }
}