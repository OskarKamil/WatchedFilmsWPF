using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace WatchedFilmsTracker.Source.Managers
{
    public enum NewFileJsonPreference
    {
        Yes,
        No,
        AskEachTime
    }

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
        public static NewFileJsonPreference JsonForNewFiles
        {
            get => settings.TryGetValue("json_for_new_files", out string? value) &&
                   Enum.TryParse(value, out NewFileJsonPreference preference) && Enum.IsDefined(preference)
                ? preference
                : NewFileJsonPreference.AskEachTime;
            set => settings["json_for_new_files"] = value.ToString();
        }

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

        public static bool RestorePreviousSession
        {
            get => settings.TryGetValue("restore_previous_session", out string? value) &&
                   bool.TryParse(value, out bool enabled) && enabled;
            set => settings["restore_previous_session"] = value.ToString();
        }

        // Retain the former API while migrating its config key on load.
        public static bool ReopenLastFile
        {
            get => RestorePreviousSession;
            set => RestorePreviousSession = value;
        }

        /// <summary>
        /// Saved tabs in their previous order. An absent list migrates the old last-file
        /// setting; an explicit empty list represents a session with no saved files.
        /// </summary>
        public static IReadOnlyList<string> SessionFilePaths
        {
            get
            {
                if (!settings.TryGetValue("session_file_paths", out string? value))
                    return NormalizeSessionPaths(new[] { LastPath });

                try
                {
                    return NormalizeSessionPaths(JsonSerializer.Deserialize<string[]>(value) ?? Array.Empty<string>());
                }
                catch (JsonException exception)
                {
                    Debug.WriteLine($"Invalid session file list: {exception.Message}");
                    return Array.Empty<string>();
                }
            }
            set => settings["session_file_paths"] = JsonSerializer.Serialize(
                NormalizeSessionPaths(value ?? Array.Empty<string>()));
        }

        public static string ActiveSessionFilePath
        {
            get => settings.TryGetValue("active_session_file_path", out string? value) ? value : LastPath;
            set => settings["active_session_file_path"] = NormalizeSessionPaths(new[] { value }).FirstOrDefault() ?? string.Empty;
        }

        public static bool FileInformationExpanded
        {
            get => bool.Parse(settings["file_information_expanded"]);
            set => settings["file_information_expanded"] = value.ToString();
        }

        public static bool SettingsExpanded
        {
            get => bool.Parse(settings["settings_expanded"]);
            set => settings["settings_expanded"] = value.ToString();
        }

        public static bool TableStructureExpanded
        {
            get => bool.Parse(settings["table_structure_expanded"]);
            set => settings["table_structure_expanded"] = value.ToString();
        }

        public static bool CollectionStatisticsExpanded
        {
            get => bool.Parse(settings["collection_statistics_expanded"]);
            set => settings["collection_statistics_expanded"] = value.ToString();
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
            settings.Clear();
            AutoSave = false;
            CheckUpdateOnStartup = false;
            DefaultDateIsToday = true;
            RestorePreviousSession = false;
            JsonForNewFiles = NewFileJsonPreference.AskEachTime;
            FileInformationExpanded = false;
            SettingsExpanded = true;
            TableStructureExpanded = true;
            CollectionStatisticsExpanded = true;
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
                bool? legacyReopenLastFile = null;
                bool hasRestorePreference = false;
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
                            case "restore_previous_session":
                                if (bool.TryParse(value, out bool restoreSession))
                                {
                                    RestorePreviousSession = restoreSession;
                                    hasRestorePreference = true;
                                }
                                else
                                    Debug.WriteLine($"Invalid bool value for {key}: {value}");
                                break;

                            case "reopen_last_file":
                                if (bool.TryParse(value, out bool reopenLastFile))
                                    legacyReopenLastFile = reopenLastFile;
                                else
                                    Debug.WriteLine($"Invalid bool value for {key}: {value}");
                                break;

                            case "session_file_paths":
                                // Preserve the key even if malformed: never revive a stale
                                // legacy last_path when an explicit session list is present.
                                settings[key] = value;
                                SessionFilePaths = SessionFilePaths;
                                break;

                            case "active_session_file_path":
                                ActiveSessionFilePath = value;
                                break;

                            case "json_for_new_files":
                                if (Enum.TryParse(value, ignoreCase: true, out NewFileJsonPreference preference) &&
                                    Enum.IsDefined(preference))
                                    JsonForNewFiles = preference;
                                else
                                    Debug.WriteLine($"Invalid JSON preference for {key}: {value}");
                                break;

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
                            case "file_information_expanded":
                            case "settings_expanded":
                            case "table_structure_expanded":
                            case "collection_statistics_expanded":
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
                if (!hasRestorePreference && legacyReopenLastFile.HasValue)
                    RestorePreviousSession = legacyReopenLastFile.Value;
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

        private static IReadOnlyList<string> NormalizeSessionPaths(IEnumerable<string> paths)
        {
            var normalized = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string? path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                try
                {
                    string fullPath = Path.GetFullPath(path);
                    if (seen.Add(fullPath))
                        normalized.Add(fullPath);
                }
                catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
                {
                    Debug.WriteLine($"Invalid session path: {exception.Message}");
                }
            }
            return normalized.ToArray();
        }
    }
}
