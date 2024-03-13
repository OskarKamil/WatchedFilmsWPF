using System;
using System.Configuration;
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

        public static string GetValues()
        {
            return $"defaultDateIsToday {DefaultDateIsToday}\nautoSave {AutoSave}\nlastPath {LastPath}";
        }

        public static void ReadSettingsFile()
        {
            Settings.Default.Reload();
            Console.WriteLine("Settings file read successfully");
        }
    }
}
