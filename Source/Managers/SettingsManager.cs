﻿using System.IO;

namespace WatchedFilmsTracker.Source.Managers
{
    public static class SettingsManager
    {
        private const string CONFIG_FILEPATH = "settings.conf";
        private static Dictionary<string, object> dictionaryNameValue = new Dictionary<string, object>();

        public static bool AutoSave
        {
            get => (bool)dictionaryNameValue[nameof(AutoSave)];
            set
            {
                dictionaryNameValue[nameof(AutoSave)] = value;
                SaveToConfFile();
            }
        }

        public static bool CheckUpdateOnStartup
        {
            get => (bool)dictionaryNameValue[nameof(CheckUpdateOnStartup)];
            set
            {
                dictionaryNameValue[nameof(CheckUpdateOnStartup)] = value;
                SaveToConfFile();
            }
        }

        public static bool DefaultDateIsToday
        {
            get => (bool)dictionaryNameValue[nameof(DefaultDateIsToday)];
            set
            {
                dictionaryNameValue[nameof(DefaultDateIsToday)] = value;
                SaveToConfFile();
            }
        }

        public static string LastPath
        {
            get => (string)dictionaryNameValue[nameof(LastPath)];
            set
            {
                dictionaryNameValue[nameof(LastPath)] = value;
                SaveToConfFile();
            }
        }

        public static bool ScrollLastPosition
        {
            get => (bool)dictionaryNameValue[nameof(ScrollLastPosition)];
            set
            {
                dictionaryNameValue[nameof(ScrollLastPosition)] = value;
                SaveToConfFile();
            }
        }

        public static double WindowHeight
        {
            get => (double)dictionaryNameValue[nameof(WindowHeight)];
            set
            {
                dictionaryNameValue[nameof(WindowHeight)] = value;
                SaveToConfFile();
            }
        }

        public static double WindowLeft
        {
            get => (double)dictionaryNameValue[nameof(WindowLeft)];
            set
            {
                dictionaryNameValue[nameof(WindowLeft)] = value;
                SaveToConfFile();
            }
        }

        public static double WindowTop
        {
            get => (double)dictionaryNameValue[nameof(WindowTop)];
            set
            {
                dictionaryNameValue[nameof(WindowTop)] = value;
                SaveToConfFile();
            }
        }

        public static double WindowWidth
        {
            get => (double)dictionaryNameValue[nameof(WindowWidth)];
            set
            {
                dictionaryNameValue[nameof(WindowWidth)] = value;
                SaveToConfFile();
            }
        }

        public static void LoadFromConfFile()
        {
            if (File.Exists(CONFIG_FILEPATH))
            {
                var lines = File.ReadAllLines(CONFIG_FILEPATH);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        if (dictionaryNameValue.ContainsKey(key))
                        {
                            if (dictionaryNameValue[key] is bool)
                            {
                                dictionaryNameValue[key] = bool.Parse(value);
                            }
                            else if (dictionaryNameValue[key] is double)
                            {
                                dictionaryNameValue[key] = double.Parse(value);
                            }
                            else if (dictionaryNameValue[key] is string)
                            {
                                dictionaryNameValue[key] = value;
                            }
                        }
                    }
                }
            }
        }

        public static void PrepareDictionary()
        {
            dictionaryNameValue.Add("AutoSave", false);
            dictionaryNameValue.Add("CheckUpdateOnStartup", false);
            dictionaryNameValue.Add("DefaultDateIsToday", true);
            dictionaryNameValue.Add("LastPath", string.Empty);
            dictionaryNameValue.Add("ScrollLastPosition", true);
            dictionaryNameValue.Add("WindowHeight", 700.0);
            dictionaryNameValue.Add("WindowLeft", 45.0);
            dictionaryNameValue.Add("WindowTop", 45.0);
            dictionaryNameValue.Add("WindowWidth", 500.0);
        }

        public static void SaveToConfFile()
        {
            var lines = new List<string>();
            lines.Add($"Generated by WatchedFilmsTracker on {DateTime.Now}");
            lines.Add(string.Empty);
            foreach (var kvp in dictionaryNameValue)
            {
                lines.Add($"{kvp.Key} = {kvp.Value}");
            }

            File.WriteAllLines(CONFIG_FILEPATH, lines);
        }
    }
}