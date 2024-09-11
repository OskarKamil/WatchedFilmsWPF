using System.Diagnostics;
using System.IO;

namespace WatchedFilmsTracker.Source.Managers
{
    /// <summary>
    /// Manages application settings.
    ///
    /// When adding a new setting:
    /// 1. Create a property with desired type and name.
    ///     1.1 Create a get with type.Parse if non-string, and access to settings dictionary with desired key string.
    ///     1.2 Use snake_case style for the key - All letters are lowercase. Words are separated by underscores (_).
    ///     1.2 Create a set that will save into dictionary under the same key string, and assign value.ToString().
    ///
    /// Example bool (non-string):
    ///     public static bool AutoSave
    /// {
    ///     get => bool.Parse(settings["auto_save"]);
    ///     set => settings["auto_save"] = value.ToString();
    /// }
    ///
    /// Example string:
    ///     public static string LastPath
    /// {
    ///     get => settings["last_path"];
    ///     set => settings["last_path"] = value;
    /// }
    ///
    /// 2. Add default value into LoadDefaultSettings() method using a setter.
    ///
    /// Example:
    /// AutoSave = false;
    ///
    /// 3 Add case "keyString" : to the LoadSettingsFromConfigFile() method under specific return type comment.
    ///
    /// Example:
    /// STRING
    /// case "last_path":
    /// case "new_setting":
    ///
    /// </summary>
    public static class SettingsManager
    {
        public static bool AutoSave
        {
            get => bool.Parse(settings["auto_save"]);
            set => settings["auto_save"] = value.ToString();
        }

        public static bool CheckUpdateOnStartup
        {
            get => bool.Parse(settings["check_update_on_startup"]);
            set => settings["check_update_on_startup"] = value.ToString();
        }

        public static bool DefaultDateIsToday
        {
            get => bool.Parse(settings["default_date_is_today"]);
            set => settings["default_date_is_today"] = value.ToString();
        }

        public static string LastPath
        {
            get => settings["last_path"];
            set => settings["last_path"] = value;
        }

        public static bool ScrollLastPosition
        {
            get => bool.Parse(settings["scroll_last_position"]);
            set => settings["scroll_last_position"] = value.ToString();
        }

        public static double WindowHeight
        {
            get => double.Parse(settings["window_height"]);
            set => settings["window_height"] = value.ToString();
        }

        public static double WindowLeft
        {
            get => double.Parse(settings["window_left"]);
            set => settings["window_left"] = value.ToString();
        }

        public static double WindowTop
        {
            get => double.Parse(settings["window_top"]);
            set => settings["window_top"] = value.ToString();
        }

        public static double WindowWidth
        {
            get => double.Parse(settings["window_width"]);
            set => settings["window_width"] = value.ToString();
        }

        private const string CONFIG_FILEPATH = "settings.conf";
        private static Dictionary<string, string> settings = new Dictionary<string, string>();

        public static void LoadDefaultSettings()
        {
            AutoSave = false;
            CheckUpdateOnStartup = false;
            DefaultDateIsToday = true;
            LastPath = "";
            ScrollLastPosition = true;
            WindowHeight = 700.0;
            WindowLeft = 45.0;
            WindowTop = 45.0;
            WindowWidth = 500.0;
        }

        public static void LoadSettingsFromConfigFile()
        {
            if (File.Exists(CONFIG_FILEPATH))
            {
                var lines = File.ReadAllLines(CONFIG_FILEPATH);
                foreach (var line in lines)
                {
                    // Split into two parts only, at the first occurrence of '='
                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = CleanKey(parts[0]);
                        var value = parts[1].Trim();

                        switch (key)
                        {
                            // DOUBLE
                            case "window_height":
                            case "window_left":
                            case "window_top":
                            case "window_width":
                                if (double.TryParse(value, out _))
                                {
                                    settings[key] = value; // Save as string anyway
                                }
                                else
                                {
                                    Debug.WriteLine($"Invalid double value for {key}: {value}");
                                }
                                break;

                            // BOOL
                            case "auto_save":
                            case "check_update_on_startup":
                            case "default_date_is_today":
                            case "scroll_last_position":
                                if (bool.TryParse(value, out _))
                                {
                                    settings[key] = value; // Save as string anyway
                                }
                                else
                                {
                                    Debug.WriteLine($"Invalid bool value for {key}: {value}");
                                }
                                break;

                            // STRING
                            case "last_path":
                                settings[key] = value; // Directly save the string
                                break;

                            default:
                                Debug.WriteLine($"Unknown setting: {key}");
                                break;
                        }
                    }
                }
            }
        }

        public static void SaveToConfFile()
        {
            var lines = new List<string>
            {
                $"Generated by WatchedFilmsTracker on {DateTime.Now}",
                string.Empty
            };
            foreach (var kvp in settings)
            {
                lines.Add($"{kvp.Key} = {kvp.Value}");
            }
            File.WriteAllLines(CONFIG_FILEPATH, lines);
        }

        private static string CleanKey(string key)
        {
            return key.Trim().ToLower().Replace(" ", "_");
        }
    }
}