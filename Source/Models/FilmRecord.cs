using System;

namespace WatchedFilmsTracker.Source.Models
{
    public class FilmRecord
    {
        private string englishTitle;
        private string originalTitle;
        private string type;
        private string releaseYear;
        private string rating;
        private string watchDate;
        private string comments;
        private int idInList = -1;

        public FilmRecord(string englishTitle, string originalTitle, string type, string releaseYear, string rating, string watchDate, string comments)
        {
            // You can add validation or conversion logic here if needed
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

        public void AddFilmRecordFromKeyboard()
        {
            Console.WriteLine("Adding film record. Please enter the following details:");
            Console.WriteLine("English title:");
            EnglishTitle = Console.ReadLine();
        }

        public string Comments
        {
            get { return comments; }
            set { comments = value; }
        }

        public string EnglishTitle
        {
            get { return englishTitle; }
            set { englishTitle = value; }
        }

        public int IdInList
        {
            get { return idInList; }
            set { idInList = value; }
        }

        public string OriginalTitle
        {
            get { return originalTitle; }
            set { originalTitle = value; }
        }

        public string Rating
        {
            get { return rating; }
            set { rating = value; }
        }

        public string ReleaseYear
        {
            get { return releaseYear; }
            set { releaseYear = value; }
        }

        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        public string WatchDate
        {
            get { return watchDate; }
            set { watchDate = value; }
        }

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
    }
}
