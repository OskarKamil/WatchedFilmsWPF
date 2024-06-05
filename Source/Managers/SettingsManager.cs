using WatchedFilmsTracker.Properties;

namespace WatchedFilmsTracker.Source.Managers
{
    public static class SettingsManager
    {
        public static bool DefaultDateIsToday
        {
            get => Settings.Default.DefaultDateIsToday;
            set
            {
                Settings.Default.DefaultDateIsToday = value;
                Settings.Default.Save();
            }
        }

        public static bool CheckUpdateOnStartup
        {
            get => Settings.Default.CheckUpdateOnStartup;
            set
            {
                Settings.Default.CheckUpdateOnStartup = value;
                Settings.Default.Save();
            }
        }

        public static bool ScrollLastPosition
        {
            get => Settings.Default.ScrollLastPosition;
            set
            {
                Settings.Default.ScrollLastPosition = value;
                Settings.Default.Save();
            }
        }

        public static double WindowTop
        {
            get => Settings.Default.WindowTop;
            set
            {
                Settings.Default.WindowTop = value;
                Settings.Default.Save();
            }
        }

        public static double WindowLeft
        {
            get => Settings.Default.WindowLeft;
            set
            {
                Settings.Default.WindowLeft = value;
                Settings.Default.Save();
            }
        }

        public static double WindowWidth
        {
            get => Settings.Default.WindowWidth;
            set
            {
                Settings.Default.WindowWidth = value;
                Settings.Default.Save();
            }
        }

        public static double WindowHeight
        {
            get => Settings.Default.WindowHeight;
            set
            {
                Settings.Default.WindowHeight = value;
                Settings.Default.Save();
            }
        }

        public static bool AutoSave
        {
            get => Settings.Default.AutoSave;
            set
            {
                Settings.Default.AutoSave = value;
                Settings.Default.Save();
            }
        }

        public static string LastPath
        {
            get => Settings.Default.LastPath;
            set
            {
                Settings.Default.LastPath = value;
                Settings.Default.Save();
            }
        }
    }
}