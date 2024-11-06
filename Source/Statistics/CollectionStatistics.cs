using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using WatchedFilmsTracker.Source.DataGridHelpers;
using WatchedFilmsTracker.Source.ManagingFilmsFile;

namespace WatchedFilmsTracker.Source.Statistics
{
    public class CollectionStatistics : INotifyPropertyChanged
    {
        public CollectionOfRecords CollectionOfRecords { get; }

        public string FormattedAverageRating
        {
            get => _formattedAverageRating;
            private set
            {
                _formattedAverageRating = value;
                OnPropertyChanged(nameof(FormattedAverageRating));
            }
        }

        public ObservableCollection<RecordModel> ObservableRecords { get; }

        public int TotalRecordsNumber
        {
            get => _totalRecordsNumber;
            private set
            {
                _totalRecordsNumber = value;
                OnPropertyChanged(nameof(TotalRecordsNumber));
            }
        }

        internal DataGridManager DataGridInfo
        {
            get => _dataGridInfo;
            set
            {
                _dataGridInfo = value;
                UpdateStatistics();
            }
        }

        private DataGridManager _dataGridInfo;
        private string _formattedAverageRating;
        private int _totalRecordsNumber;

        public CollectionStatistics(CollectionOfRecords recordList)
        {
            CollectionOfRecords = recordList;
            ObservableRecords = recordList.ObservableCollectionOfRecords;
            ObservableRecords.CollectionChanged += Collection_RecordHasChanged;
            CollectionOfRecords.RecordHasChanged += Collection_RecordHasChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static string GetFormattedRating(double rating)
        {
            string formattedRating = rating.ToString("#.00") + "/4";
            return formattedRating;
        }

        private void Collection_RecordHasChanged(object? sender, EventArgs e)
        {
            Debug.WriteLine("best place, it should update statistics");
            UpdateStatistics();
        }

        private string GetFormattedAverageRating()
        {
            int ratingColumnId = DataGridInfo.GetIdOfColumnByHeader("Rating");
            if (ratingColumnId == -1)
            {
                return "\"Rating\" column not found";
            }

            if (ObservableRecords.Count == 0)
                return "0.00/4";

            double totalRating = 0;
            int validRecords = 0;

            foreach (var record in ObservableRecords)
            {
                if (double.TryParse(record.Cells[ratingColumnId].Value, out double rating))
                {
                    totalRating += rating;
                    validRecords++;
                }
            }

            double averageRating = validRecords > 0 ? totalRating / validRecords : 0;
            return averageRating.ToString("0.00") + "/4";
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //public double GetAverageFilmRating()
        //{
        //    double averageRating = 0;
        //    foreach (RecordModel record in ObservableRecords)
        //    {
        //        try
        //        {
        //            averageRating += double.Parse(film.Rating);
        //        }
        //        catch (Exception)
        //        {
        //            correction++;
        //        }
        //    }
        //    averageRating /= listOfFilms.Count - correction;
        //    return averageRating;
        //}
        private void UpdateStatistics()
        {
            TotalRecordsNumber = ObservableRecords.Count;
            FormattedAverageRating = GetFormattedAverageRating();
        }

        //public string GetAverageWatchStatistics()
        //{
        //    //  not in use for now
        //    if (filmRecords.Count == 0)
        //        return "0";

        //    List<DateTime> dates = new List<DateTime>();
        //    int correction = 0;

        //    foreach (FilmRecord film in filmRecords)
        //    {
        //        try
        //        {
        //            DateTime date = DateTime.ParseExact(film.WatchDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        //            dates.Add(date);
        //        }
        //        catch (Exception)
        //        {
        //            correction++;
        //            //doesn't do anything yet, but may indicate not valid film so can correct number of films and days
        //        }
        //    }

        //    if (dates.Count == 0) // No dates found
        //        return "0";

        //    dates.Sort();

        //    int daysBetween = (int)(dates.Last() - dates.First()).TotalDays + 1;
        //    int numberOfFilms = GetNumberOfTotalWatchedFilms();
        //    string statisticsString = "";

        //    string averageFilmPerDay = ((double)numberOfFilms / daysBetween).ToString("#.##");
        //    statisticsString += averageFilmPerDay + " films per day";

        //    return statisticsString;
        //}

        //public async Task<ObservableCollection<DecadalStatistic>> GetDecadalReport(CancellationToken cancellationToken)
        //{
        //    /* Very heavy method. Instead of generating new dictionaries and collections each time a property of film record is changed, generate them when needed. For example generate years and decades and keep track of them when a list or property is being changed. Check TODO.txt file.  */

        //    return await Task.Run(async () =>
        //    {
        //        ObservableCollection<DecadalStatistic> decadalStatistics = new ObservableCollection<DecadalStatistic>();

        //        var dictionary = GetDecadalDictionary();
        //        // await Task.Delay(1000);
        //        foreach (var decadeGroup in dictionary)
        //        {
        //            cancellationToken.ThrowIfCancellationRequested();

        //            Collection<FilmRecord> filmsInDecade = new Collection<FilmRecord>(decadeGroup.Value);
        //            int decade = decadeGroup.Key;
        //            int numberOfFilms = StatisticsManager.GetNumberOfTotalWatchedFilms(filmsInDecade);
        //            double averageRating = StatisticsManager.GetAverageFilmRating(filmsInDecade);
        //            decadalStatistics.Add(new DecadalStatistic(decade, numberOfFilms, averageRating));
        //            await Task.Delay(1, cancellationToken);
        //        }

        //        return decadalStatistics;
        //    }, cancellationToken);
        //}

        //public async Task<ObservableCollection<YearlyStatistic>> GetYearlyReport(CancellationToken cancellationToken)
        //{
        //    /* Very heavy method. Instead of generating new dictionaries and collections each time a property of film record is changed, generate them when needed. For example generate years and decades and keep track of them when a list or property is being changed. Check TODO.txt file.  */

        //    return await Task.Run(async () =>
        //    {
        //        ObservableCollection<YearlyStatistic> yearlyStatistics = new ObservableCollection<YearlyStatistic>();

        //        var dictionary = GetYearyDictionary();
        //        // await Task.Delay(1000);
        //        foreach (var yearGroup in dictionary)
        //        {
        //            cancellationToken.ThrowIfCancellationRequested();

        //            Collection<FilmRecord> filmsInYear = new Collection<FilmRecord>(yearGroup.Value);
        //            int year = yearGroup.Key;
        //            int numberOfFilms = StatisticsManager.GetNumberOfTotalWatchedFilms(filmsInYear);
        //            double averageRating = StatisticsManager.GetAverageFilmRating(filmsInYear);
        //            yearlyStatistics.Add(new YearlyStatistic(year, numberOfFilms, averageRating));

        //            await Task.Delay(1, cancellationToken);
        //        }

        //        return yearlyStatistics;
        //    }, cancellationToken);
        //}

        //private Dictionary<int, List<FilmRecord>> GetDecadalDictionary()
        //{
        //    List<FilmRecord> sortedFilms = filmRecords.OrderBy(o => o.ReleaseYear).ToList();
        //    Dictionary<int, List<FilmRecord>> decades = new Dictionary<int, List<FilmRecord>>();

        //    foreach (FilmRecord film in sortedFilms)
        //    {
        //        int filmYear;
        //        double filmRating;

        //        try
        //        {
        //            filmYear = int.Parse(film.ReleaseYear);
        //            filmRating = double.Parse(film.Rating);
        //        }
        //        catch (Exception e)
        //        {
        //            continue;
        //        }

        //        int decade = (filmYear / 10) * 10;

        //        if (!decades.ContainsKey(decade))
        //        {
        //            decades[decade] = new List<FilmRecord> { film };
        //        }
        //        else
        //        {
        //            decades[decade].Add(film);
        //        }
        //    }
        //    return decades;
        //}

        //private Dictionary<int, List<FilmRecord>> GetYearyDictionary()
        //{
        //    List<FilmRecord> sortedFilms = filmRecords.OrderBy(o => o.ReleaseYear).ToList();
        //    Dictionary<int, List<FilmRecord>> years = new Dictionary<int, List<FilmRecord>>();

        //    foreach (FilmRecord film in sortedFilms)
        //    {
        //        int watchedYear;
        //        if (!(film.WatchDate == null) && film.WatchDate.Length >= 4)
        //        {
        //            try
        //            {
        //                watchedYear = int.Parse(film.WatchDate.Substring(film.WatchDate.Length - 4));
        //            }
        //            catch (Exception e)
        //            {
        //                continue;
        //            }

        //            if (!years.ContainsKey(watchedYear))
        //            {
        //                years[watchedYear] = new List<FilmRecord> { film };
        //            }
        //            else
        //            {
        //                years[watchedYear].Add(film);
        //            }
        //        }
        //    }

        //    var sortedYears = years.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);

        //    return sortedYears;
        //}
    }
}