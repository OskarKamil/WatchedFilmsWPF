using System.Windows;
using System.Windows.Controls;
using WatchedFilmsTracker.Source.ManagingFilmsFile;
using WatchedFilmsTracker.Source.Statistics;
using WatchedFilmsTracker.Source.ViewModels;

namespace WatchedFilmsTracker.Source.Views
{
    /// <summary>Displays statistics; each open collection retains its own editor and results.</summary>
    public partial class StatisticsPanel : UserControl
    {
        private readonly Dictionary<CollectionFile, StatisticsSession> sessions = new();

        public StatisticsPanel()
        {
            InitializeComponent();
            IsEnabled = false;
            DefaultStatisticsComboBox.ItemsSource = DefaultStatisticsCatalog.All;
            Unloaded += OnUnloaded;
        }

        private StatisticsSession? Session => DataContext as StatisticsSession;

        public void ShowCollection(CollectionFile? collectionFile)
        {
            if (collectionFile == null)
            {
                DataContext = null;
                IsEnabled = false;
                return;
            }

            if (!sessions.TryGetValue(collectionFile, out StatisticsSession? session))
            {
                session = new StatisticsSession(collectionFile);
                sessions.Add(collectionFile, session);
                collectionFile.FileClosing += CollectionClosing;
            }

            DataContext = session;
            IsEnabled = true;
            _ = session.InitializeAsync();
        }

        public Task RefreshAsync() => Session?.RefreshAsync() ?? Task.CompletedTask;

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

        private async void AddDraft_Click(object sender, RoutedEventArgs e)
        {
            if (Session != null)
                await Session.AddDraftAsync();
        }

        private async void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (Session != null)
                await Session.MoveSelectedAsync(-1);
        }

        private async void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (Session != null)
                await Session.MoveSelectedAsync(1);
        }

        private async void SaveAndRun_Click(object sender, RoutedEventArgs e)
        {
            if (Session != null)
                await Session.SaveAndRunAsync();
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (Session != null)
                await Session.DeleteSelectedAsync();
        }

        private void CollectionClosing(object? sender, EventArgs e)
        {
            if (sender is CollectionFile collectionFile && sessions.Remove(collectionFile, out StatisticsSession? session))
            {
                collectionFile.FileClosing -= CollectionClosing;
                session.Dispose();
                if (ReferenceEquals(DataContext, session))
                    DataContext = null;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            foreach ((CollectionFile collectionFile, StatisticsSession session) in sessions)
            {
                collectionFile.FileClosing -= CollectionClosing;
                session.Dispose();
            }

            sessions.Clear();
            DataContext = null;
        }
    }
}
