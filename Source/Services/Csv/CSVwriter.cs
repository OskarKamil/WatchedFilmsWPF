using System.Diagnostics;
using System.IO;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Services.Csv
{
    public class CSVwriter
    {
        private string fileColumn;
        private string filePath;
        private StreamWriter filmsWriter;

        public CSVwriter(string newFilePath)
        {
            try
            {
                filmsWriter = new StreamWriter(newFilePath, false, System.Text.Encoding.UTF8);
            }
            catch (IOException e)
            {
                Console.Error.WriteLine("File not found.");
            }
        }

        public void Close()
        {
            filmsWriter.Close();
        }

        public void SaveListIntoCSV(List<FilmRecord> list)
        {
            filmsWriter.WriteLine(fileColumn);
            foreach (FilmRecord filmRecord in list)
            {
                filmsWriter.Write(ToString(filmRecord.EnglishTitle) + "\t");
                filmsWriter.Write(ToString(filmRecord.OriginalTitle) + "\t");
                filmsWriter.Write(ToString(filmRecord.Type) + "\t");
                filmsWriter.Write(ToString(filmRecord.ReleaseYear) + "\t");
                filmsWriter.Write(ToString(filmRecord.Rating) + "\t");
                filmsWriter.Write(ToString(filmRecord.WatchDate) + "\t");
                filmsWriter.Write(ToString(filmRecord.Comments) + "\n");
            }
            filmsWriter.Close();
            Debug.WriteLine("Saved and closed");
        }

        public void SetFileColumn(string fileColumns)
        {
            fileColumn = fileColumns;
        }

        private string ToString(string value)
        {
            return value ?? "";
        }
    }
}