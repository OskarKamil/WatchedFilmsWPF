using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.ManagingFilmsFile;

namespace WatchedFilmsTracker.Source.Managers
{
    internal class SearchManager
    {
        private string defaultSearchText;
        private DataGrid filmsGrid;
        private TextBox searchTextBox;
        private WorkingTextFile WorkingTextFile;

        public SearchManager(WorkingTextFile fileManager, TextBox searchTextBox, DataGrid filmsGrid)
        {
            this.WorkingTextFile = fileManager;
            this.searchTextBox = searchTextBox;
            this.filmsGrid = filmsGrid;
            defaultSearchText = searchTextBox.Text;

            this.WorkingTextFile.CollectionHasChanged += FileManager_AnyChangeHappenedEvent;
        }

        public void SearchFilms()
        {
            string searchPhrase = searchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(searchPhrase) || searchTextBox.Text == defaultSearchText)
            {
                filmsGrid.ItemsSource = WorkingTextFile.GetObservableCollectionOfRecords();
                return;
            }

            var filteredList = new ObservableCollection<RecordModel>();

            foreach (var filmRecord in WorkingTextFile.GetObservableCollectionOfRecords())
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