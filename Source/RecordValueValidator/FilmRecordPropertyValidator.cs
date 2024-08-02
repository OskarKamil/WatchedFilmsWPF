using System.Diagnostics;

namespace WatchedFilmsTracker.Source.RecordValueValidator
{
    internal class FilmRecordPropertyValidator
    {
        public static bool Validate(string columnHeader, string value)
        {
            switch (columnHeader.ToLower())
            {
                case "Release year":
                    if (int.TryParse(value, out int releaseYear))
                    {
                        if (releaseYear > 1900 && releaseYear <= System.DateTime.Now.Year + 5)
                        {
                            return true;
                        }
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                default:
                    return true;
            }
        }

        internal bool IsReleaseYearValid(string releaseYearString)
        {
            if (releaseYearString == null) return false;

            if (int.TryParse(releaseYearString, out int releaseYear))
            {
                if (releaseYear > 1900 && releaseYear <= System.DateTime.Now.Year + 5)
                {
                    Debug.WriteLine("FilmRecordPropertyValidator we are here");

                    return true;
                }
                return false;
            }
            else
            {
                return false;
            }
        }
    }
}