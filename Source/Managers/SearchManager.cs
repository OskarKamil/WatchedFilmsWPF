using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.ManagingFilmsFile;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Managers
{
    internal class SearchManager
    {
        private string defaultSearchText;
        private FilmsTextFile fileManager;
        private DataGrid filmsGrid;
        private TextBox searchTextBox;

        public SearchManager(FilmsTextFile fileManager, TextBox searchTextBox, DataGrid filmsGrid)
        {
            this.fileManager = fileManager;
            this.searchTextBox = searchTextBox;
            this.filmsGrid = filmsGrid;
            defaultSearchText = searchTextBox.Text;

            this.fileManager.AnyChangeHappenedEvent += FileManager_AnyChangeHappenedEvent;
        }

        public void SearchFilms()
        {
            string searchPhrase = searchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(searchPhrase) || searchTextBox.Text == defaultSearchText)
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
                    if (property.Name == "WatchDate") // ignored search columns
                        return false;
                    return value != null && value.ToString().ToLowerInvariant().Contains(searchPhrase.ToLowerInvariant());
                });
                if (marchFound)
                {
                    filteredList.Add(filmRecord);
                }
            }

            filmsGrid.ItemsSource = filteredList;
        }

        private void FileManager_AnyChangeHappenedEvent(object sender, EventArgs e)
        {
            SearchFilms();
        }
    }
}