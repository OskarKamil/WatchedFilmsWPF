using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using WatchedFilmsTracker.Source.ManagingFilmsFile;
using WatchedFilmsTracker.Source.ManagingRecords;
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
            Unloaded += OnUnloaded;
        }

        private StatisticsSession? Session => DataContext as StatisticsSession;

        public void ShowCollection(CollectionFile? collectionFile)
        {
            CloseMenus();
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

        private void ShowStatisticTemplates_Click(object sender, RoutedEventArgs e)
        {
            if (Session is not StatisticsSession session)
                return;

            var menu = CreateMenu(AddStatisticButton);
            foreach (StatisticDefinition template in DefaultStatisticsCatalog.All)
            {
                var item = new MenuItem { Header = template.Name, Tag = template, ToolTip = template.Sql };
                item.Click += async (_, _) =>
                {
                    menu.IsOpen = false;
                    if (ReferenceEquals(Session, session))
                        await session.AddDefaultAsync(template);
                };
                menu.Items.Add(item);
            }
            menu.IsOpen = true;
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

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (Session != null)
                await Session.DeleteSelectedAsync();
        }

        private void ShowDeletedStatistics_Click(object sender, RoutedEventArgs e)
        {
            if (Session is not StatisticsSession session || !session.HasDeletedStatistics)
                return;

            var menu = CreateMenu(RestoreStatisticsButton);
            foreach (var deleted in session.RecentlyDeletedStatistics)
            {
                var item = new MenuItem { Header = deleted.DisplayText, Tag = deleted, ToolTip = deleted.Definition.Sql };
                item.Click += async (_, _) =>
                {
                    menu.IsOpen = false;
                    if (ReferenceEquals(Session, session))
                    {
                        session.SelectedDeletedStatistic = deleted;
                        await session.RestoreSelectedAsync();
                    }
                };
                menu.Items.Add(item);
            }
            menu.IsOpen = true;
        }

        private static ContextMenu CreateMenu(Button button)
        {
            if (button.ContextMenu != null)
                button.ContextMenu.IsOpen = false;

            var headerText = new FrameworkElementFactory(typeof(TextBlock));
            headerText.SetBinding(TextBlock.TextProperty, new Binding());
            headerText.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            headerText.SetValue(FrameworkElement.MaxWidthProperty, 300.0);
            var itemStyle = new Style(typeof(MenuItem));
            itemStyle.Setters.Add(new Setter(HeaderedItemsControl.HeaderTemplateProperty,
                new DataTemplate { VisualTree = headerText }));
            var menu = new ContextMenu
            {
                PlacementTarget = button,
                Placement = PlacementMode.Bottom,
                MaxHeight = 360,
                ItemContainerStyle = itemStyle,
            };
            button.ContextMenu = menu;
            return menu;
        }

        private void CloseMenus()
        {
            if (AddStatisticButton.ContextMenu != null)
                AddStatisticButton.ContextMenu.IsOpen = false;
            if (RestoreStatisticsButton.ContextMenu != null)
                RestoreStatisticsButton.ContextMenu.IsOpen = false;
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
            CloseMenus();
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
