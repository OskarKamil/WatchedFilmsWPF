using System.ComponentModel;
using WatchedFilmsTracker.Source.DataGridHelpers;

namespace WatchedFilmsTracker.Source.Models
{
    public class FilmRecord : INotifyPropertyChanged
    {
        public List<Cell> Cells { get; set; } = new List<Cell>();

        public string Comments
        {
            get { return comments; }
            set
            {
                if (comments != value)
                {
                    comments = value;
                    OnPropertyChanged(nameof(Comments));
                }
            }
        }

        public string EnglishTitle
        {
            get { return englishTitle; }
            set
            {
                if (englishTitle != value)
                {
                    englishTitle = value;
                    OnPropertyChanged(nameof(EnglishTitle));
                }
            }
        }

        public int IdInList
        {
            get { return idInList; }
            set
            {
                if (idInList != value)
                {
                    idInList = value;
                    OnPropertyChanged(nameof(IdInList));
                }
            }
        }

        public string OriginalTitle
        {
            get { return originalTitle; }
            set
            {
                if (originalTitle != value)
                {
                    originalTitle = value;
                    OnPropertyChanged(nameof(OriginalTitle));
                }
            }
        }

        public string Rating
        {
            get { return rating; }
            set
            {
                if (rating != value)
                {
                    rating = value;
                    OnPropertyChanged(nameof(Rating));
                }
            }
        }

        public string ReleaseYear
        {
            get { return releaseYear; }
            set
            {
                if (releaseYear != value)
                {
                    releaseYear = value;
                    OnPropertyChanged(nameof(ReleaseYear));
                }
            }
        }

        public string Type
        {
            get { return type; }
            set
            {
                if (type != value)
                {
                    type = value;
                    OnPropertyChanged(nameof(Type));
                }
            }
        }

        public string WatchDate
        {
            get { return watchDate; }
            set
            {
                if (watchDate != value)
                {
                    watchDate = value;
                    OnPropertyChanged(nameof(WatchDate));
                }
            }
        }

        private string comments;

        private string englishTitle;

        private int idInList = -1;

        private string originalTitle;

        private string rating;

        private string releaseYear;

        private string type;

        private string watchDate;

        public FilmRecord(List<Cell> cells)
        {
            Cells = cells;
        }

        public FilmRecord(string englishTitle, string originalTitle, string type, string releaseYear, string rating, string watchDate, string comments)
        {
            this.englishTitle = englishTitle;
            this.originalTitle = originalTitle;
            this.type = type;
            this.releaseYear = releaseYear;
            this.rating = rating;
            this.watchDate = watchDate;
            this.comments = comments;
        }

        public FilmRecord()
        {
        }

        public FilmRecord(int id)
        {
            idInList = id;
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public string ToNiceString()
        {
            return $"{englishTitle}\t{originalTitle}\t{type}\t{releaseYear}\t{rating}\t{watchDate}\t{comments}";
        }

        public string ToNiceString2()
        {
            return $"{englishTitle}, {originalTitle}, {type}, {releaseYear}, {rating}, {watchDate}, {comments}";
        }

        public override string ToString()
        {
            return $"FilmRecord{{englishTitle='{englishTitle}', originalTitle='{originalTitle}', type='{type}', releaseYear='{releaseYear}', rating='{rating}', watchDate='{watchDate}', comments='{comments}', idInList={idInList}}}";
        }

        public string ToNewString()
        {
            return "wip";
            //todo all
        }

        public string StringToBeSavedInFile()
        {
            return "wip";
            //todo check which columns exist and what order
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}