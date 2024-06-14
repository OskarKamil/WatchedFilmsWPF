using System.Collections.ObjectModel;
using System.Globalization;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Managers
{
    internal class StatisticsManager
    {
        private ObservableCollection<DecadalStatistic> decadesList;
        private ObservableCollection<FilmRecord> filmRecords;

        public StatisticsManager(ObservableCollection<FilmRecord> filmsObservableList)
        {
            filmRecords = filmsObservableList;
        }

        public static String FormattedRating(double rating)
        {
            string formattedRating = rating.ToString("#.00") + "/4";
            return formattedRating;
        }

        public static double GetAverageFilmRating(Collection<FilmRecord> listOfFilms)
        {
            double averageRating = 0;
            int correction = 0;
            foreach (FilmRecord film in listOfFilms)
            {
                try
                {
                    averageRating += double.Parse(film.Rating);
                }
                catch (Exception e)
                {
                    correction++;
                }
            }
            averageRating /= listOfFilms.Count - correction;

            return averageRating;
        }

        public static int GetNumberOfTotalWatchedFilms(Collection<FilmRecord> listOfFilms)
        {
            return listOfFilms.Count;
        }

        public double GetAverageFilmRating()
        {
            return GetAverageFilmRating(filmRecords);
        }

        public string GetAverageWatchStatistics()
        {
            //  not in use for now
            if (filmRecords.Count == 0)
                return "0";

            List<DateTime> dates = new List<DateTime>();
            int correction = 0;

            foreach (FilmRecord film in filmRecords)
            {
                try
                {
                    DateTime date = DateTime.ParseExact(film.WatchDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    dates.Add(date);
                }
                catch (Exception e)
                {
                    correction++;
                    //doesn't do anything yet, but may indicate not valid film so can correct number of films and days
                }
            }

            if (dates.Count == 0) // No dates found
                return "0";

            dates.Sort();

            int daysBetween = (int)(dates.Last() - dates.First()).TotalDays + 1;
            int numberOfFilms = GetNumberOfTotalWatchedFilms();
            string statisticsString = "";

            string averageFilmPerDay = ((double)numberOfFilms / daysBetween).ToString("#.##");
            statisticsString += averageFilmPerDay + " films per day";

            return statisticsString;
        }

        public async Task<ObservableCollection<DecadalStatistic>> GetDecadalReport(CancellationToken cancellationToken)
        {
            /* Very heavy method. Instead of generating new dictionaries and collections each time a property of film record is changed, generate them when needed. For example generate years and decades and keep track of them when a list or property is being changed. Check TODO.txt file.  */

            return await Task.Run(() =>
            {
                ObservableCollection<DecadalStatistic> decadalStatistics = new ObservableCollection<DecadalStatistic>();

                var dictionary = GetDecadalDictionary();
                // await Task.Delay(1000);
                foreach (var decadeGroup in dictionary)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Collection<FilmRecord> filmsInDecade = new Collection<FilmRecord>(decadeGroup.Value);
                    int decade = decadeGroup.Key;
                    int numberOfFilms = StatisticsManager.GetNumberOfTotalWatchedFilms(filmsInDecade);
                    double averageRating = StatisticsManager.GetAverageFilmRating(filmsInDecade);
                    decadalStatistics.Add(new DecadalStatistic(decade, numberOfFilms, averageRating));
                }

                return decadalStatistics;
            }, cancellationToken);
        }

        public int GetNumberOfTotalWatchedFilms()
        {
            return GetNumberOfTotalWatchedFilms(filmRecords);
        }

        public async Task<ObservableCollection<YearlyStatistic>> GetYearlyReport(CancellationToken cancellationToken)
        {
            /* Very heavy method. Instead of generating new dictionaries and collections each time a property of film record is changed, generate them when needed. For example generate years and decades and keep track of them when a list or property is being changed. Check TODO.txt file.  */

            return await Task.Run(() =>
            {
                ObservableCollection<YearlyStatistic> yearlyStatistics = new ObservableCollection<YearlyStatistic>();

                var dictionary = GetYearyDictionary();
                // await Task.Delay(1000);
                foreach (var yearGroup in dictionary)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Collection<FilmRecord> filmsInYear = new Collection<FilmRecord>(yearGroup.Value);
                    int year = yearGroup.Key;
                    int numberOfFilms = StatisticsManager.GetNumberOfTotalWatchedFilms(filmsInYear);
                    double averageRating = StatisticsManager.GetAverageFilmRating(filmsInYear);
                    yearlyStatistics.Add(new YearlyStatistic(year, numberOfFilms, averageRating));
                }

                return yearlyStatistics;
            }, cancellationToken);
        }

        private Dictionary<int, List<FilmRecord>> GetDecadalDictionary()
        {
            List<FilmRecord> sortedFilms = filmRecords.OrderBy(o => o.ReleaseYear).ToList();
            Dictionary<int, List<FilmRecord>> decades = new Dictionary<int, List<FilmRecord>>();

            foreach (FilmRecord film in sortedFilms)
            {
                int filmYear;
                double filmRating;

                try
                {
                    filmYear = int.Parse(film.ReleaseYear);
                    filmRating = double.Parse(film.Rating);
                }
                catch (Exception e)
                {
                    continue;
                }

                int decade = (filmYear / 10) * 10;

                if (!decades.ContainsKey(decade))
                {
                    decades[decade] = new List<FilmRecord> { film };
                }
                else
                {
                    decades[decade].Add(film);
                }
            }
            return decades;
        }

        private Dictionary<int, List<FilmRecord>> GetYearyDictionary()
        {
            List<FilmRecord> sortedFilms = filmRecords.OrderBy(o => o.ReleaseYear).ToList();
            Dictionary<int, List<FilmRecord>> years = new Dictionary<int, List<FilmRecord>>();

            foreach (FilmRecord film in sortedFilms)
            {
                int watchedYear;
                if (!(film.WatchDate == null) && film.WatchDate.Length >= 4)
                {
                    try
                    {
                        watchedYear = int.Parse(film.WatchDate.Substring(film.WatchDate.Length - 4));
                    }
                    catch (Exception e)
                    {
                        continue;
                    }

                    if (!years.ContainsKey(watchedYear))
                    {
                        years[watchedYear] = new List<FilmRecord> { film };
                    }
                    else
                    {
                        years[watchedYear].Add(film);
                    }
                }
            }

            var sortedYears = years.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value);

            return sortedYears;
        }
    }
}