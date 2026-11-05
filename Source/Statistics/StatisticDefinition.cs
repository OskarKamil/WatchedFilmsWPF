using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WatchedFilmsTracker.Source.Statistics
{
    public sealed class StatisticDefinition : INotifyPropertyChanged
    {
        private string _name = "New statistic";
        private string _sql = "SELECT COUNT(*) AS \"Total records\" FROM records";

        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;

                _name = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string Sql
        {
            get => _sql;
            set
            {
                if (_sql == value)
                    return;

                _sql = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public StatisticDefinition Copy() => new()
        {
            Name = Name,
            Sql = Sql,
        };

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
