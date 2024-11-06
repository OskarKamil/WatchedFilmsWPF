namespace WatchedFilmsTracker.Source.Statistics
{
    public class YearlyStatistic
    {
        private double averageRating;
        private string averageRatingString;

        private int numberOfFilms;
        private int year;

        public YearlyStatistic(int year, int numberOfFilms, double averageRating)
        {
            this.year = year;
            this.numberOfFilms = numberOfFilms;
            AverageRating = averageRating;
        }

        public double AverageRating
        {
            get => averageRating; set
            {
                averageRating = value;
                averageRatingString = CollectionStatistics.GetFormattedRating(averageRating);
            }
        }

        public string AverageRatingString { get => averageRatingString; }

        public int NumberOfFilms { get => numberOfFilms; set => numberOfFilms = value; }

        public int Year
        {
            get => year;
            set
            {
                year = value;
            }
        }
    }
}