using System.Collections.ObjectModel;
using System.Globalization;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Managers
{
    internal class StatisticsManager
    {
        private ObservableCollection<FilmRecord> filmRecords;

        public StatisticsManager(ObservableCollection<FilmRecord> filmsObservableList)
        {
            filmRecords = filmsObservableList;
        }

        public String FormattedRating(double rating)
        {
            string formattedRating = rating.ToString("#.##") + "/4";
            return formattedRating;
        }

        public Dictionary<int, List<FilmRecord>> GetAllDecadesStatistics()
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
            //var sortedDecades = decades.OrderBy(d => d.Key);
            //films were sorted before so decades should be chronological by now

            return decades;
        }

        public double GetAverageFilmRating()
        {
            return GetAverageFilmRating(filmRecords);
        }

        public double GetAverageFilmRating(Collection<FilmRecord> listOfFilms)
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

        public string GetAverageWatchStatistics()
        {
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

        public void GetAverageWatchStatisticsByYear()
        {
            //throw new NotImplementedException();
        }

        public int GetNumberOfTotalWatchedFilms()
        {
            return GetNumberOfTotalWatchedFilms(filmRecords);
        }

        public int GetNumberOfTotalWatchedFilms(Collection<FilmRecord> listOfFilms)
        {
            return listOfFilms.Count;
        }
    }
}