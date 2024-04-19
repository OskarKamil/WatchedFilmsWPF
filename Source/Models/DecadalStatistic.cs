using WatchedFilmsTracker.Source.Managers;

namespace WatchedFilmsTracker.Source.Models
{
    internal class DecadalStatistic
    {
        private double averageRating;
        private string averageRatingString;
        private int decade;
        private string decadeString;
        private int numberOfFilms;

        public DecadalStatistic(int decade, int numberOfFilms, double averageRating)
        {
            this.Decade = decade;
            this.numberOfFilms = numberOfFilms;
            this.AverageRating = averageRating;
        }

        public double AverageRating
        {
            get => averageRating; set
            {
                averageRating = value;
                averageRatingString = StatisticsManager.FormattedRating(averageRating);
            }
        }

        public string AverageRatingString { get => averageRatingString; }

        public int Decade
        {
            get => decade;
            set
            {
                decade = value;
                decadeString = decade + "s";
            }
        }

        public string DecadeString { get => decadeString; }
        public int NumberOfFilms { get => numberOfFilms; set => numberOfFilms = value; }
    }
}