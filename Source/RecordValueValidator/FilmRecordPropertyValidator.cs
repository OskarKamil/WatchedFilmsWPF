using System.Diagnostics;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.RecordValueValidator
{
    internal class FilmRecordPropertyValidator
    {
        private DataGrid _visualFilmsTable;
        private int _wrongReleaseYearCount;
        //todo add local val for number of wroong ones so can be summed up

        public FilmRecordPropertyValidator(DataGrid visualFilmsTable)
        {
            _visualFilmsTable = visualFilmsTable;
            _wrongReleaseYearCount = 0;
        }

        internal bool IsReleaseYearValid(FilmRecord? filmRecord)
        {
            if (filmRecord == null) return false;

            if (int.TryParse(filmRecord.ReleaseYear, out int releaseYear))
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