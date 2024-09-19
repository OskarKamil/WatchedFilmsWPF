using System.ComponentModel;
using WatchedFilmsTracker.Source;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private int _totalFilmsWatched;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ProgramVersion => ProgramInformation.VERSION;
    public string ProgramCopyright => ProgramInformation.COPYRIGHT;

    public int TotalFilmsWatched
    {
        get => _totalFilmsWatched;
        set
        {
            if (_totalFilmsWatched != value)
            {
                _totalFilmsWatched = value;
                OnPropertyChanged(nameof(TotalFilmsWatched));
            }
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
