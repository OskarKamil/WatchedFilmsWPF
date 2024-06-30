using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Managers
{
    internal class SearchManager
    {
        private FileManager fileManager;
        private DataGrid filmsGrid;
        private TextBox searchTextBox;

        public SearchManager(FileManager fileManager, TextBox searchTextBox, DataGrid filmsGrid)
        {
            this.fileManager = fileManager;
            this.searchTextBox = searchTextBox;
            this.filmsGrid = filmsGrid;
        }

        //todo works
        // sometimes works before a key is pressed
        // works for watch dates too, but may be misleading
        // make it work only when enter is pressed, or give option to choose
        // exclude watch dates from search
        // check how adding, and removing films affects search
        // check how opening new file or revert changes affects the search

        public void SearchFilms()
        {
            string searchPhrase = searchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(searchPhrase))
            {
                filmsGrid.ItemsSource = fileManager.FilmsObservableList;
                return;
            }

            var filteredList = new ObservableCollection<FilmRecord>();

            foreach (var filmRecord in fileManager.FilmsObservableList)
            {
                var properties = filmRecord.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                bool marchFound = properties.Any(property =>
                {
                    var value = property.GetValue(filmRecord);
                    return value != null && value.ToString().ToLowerInvariant().Contains(searchPhrase.ToLowerInvariant());
                });
                if (marchFound)
                {
                    filteredList.Add(filmRecord);
                }
            }

            filmsGrid.ItemsSource = filteredList;
        }
    }
}