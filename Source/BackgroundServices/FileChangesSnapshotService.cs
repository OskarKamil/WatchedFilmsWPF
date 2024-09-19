using System.Diagnostics;
using System.IO;
using WatchedFilmsTracker.Source.ManagingFilmsFile;

namespace WatchedFilmsTracker.Source.BackgroundServices
{
    internal class FileChangesSnapshotService
    {
        public const int MAX_NUMBER_OF_SNAPSHOTS = 5;
        private static WorkingTextFile fileManager;
        private static string filePattern = "snapshot_*.txt";
        private static string snapshotDirectory = "";
        private static string[] snapshotFiles = null;
        internal static WorkingTextFile FileManager { get => fileManager; set => fileManager = value; }

        public static void CreateNewSnapshot(CollectionOfRecords filmsFile)
        {
            filmsFile.StartWriter(snapshotDirectory + "/" + $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            int numberOfSnapshots = GetSnapshotFiles().Length;
            if (numberOfSnapshots > MAX_NUMBER_OF_SNAPSHOTS)
                DeleteOldestSnapshots(numberOfSnapshots - MAX_NUMBER_OF_SNAPSHOTS);
        }

        public static void CreateSnapshotFolderIfNotExist()
        {
            string currentDirectory = Environment.CurrentDirectory;
            snapshotDirectory = Path.Combine(currentDirectory, "filesSnapshot");

            if (!Directory.Exists(snapshotDirectory))
                Directory.CreateDirectory(snapshotDirectory);
        }

        public static void DeleteOldestSnapshots(int number)
        {
            try
            {
                List<(string fileName, DateTime dateTime)> fileDateTimeList = new List<(string fileName, DateTime dateTime)>();
                foreach (string file in snapshotFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string dateText = fileName.Replace("snapshot_", "");

                    if (DateTime.TryParseExact(dateText, "yyyyMMdd_HHmmss", null, System.Globalization.DateTimeStyles.None, out DateTime dateTime))
                    {
                        fileDateTimeList.Add((file, dateTime));
                    }
                }

                Debug.WriteLine($"Length of {nameof(fileDateTimeList)} list: {fileDateTimeList.Count}");

                var sortedFiles = fileDateTimeList.OrderBy(x => x.dateTime).Select(x => x.fileName).ToArray();
                for (int i = 0; i < number; i++)
                {
                    File.Delete(sortedFiles[i]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occured: {ex.Message}");
            }
        }

        public static string[] GetSnapshotFiles()
        {
            try
            {
                snapshotFiles = Directory.GetFiles(snapshotDirectory, filePattern);
            }
            catch (Exception ex)
            {
                Debug.Print($"An error occured: {ex.Message}");
            }
            Debug.WriteLine($"number of snapshots: {snapshotFiles.Length}");

            return snapshotFiles;
        }

        public static void SubscribeToSaveCompletedEvent(MainWindow mainWindow)
        {
            //FileManager.SavedComplete += HandleSaveCompleted;
        }

        private static void HandleSaveCompleted(object sender, CollectionOfRecords filmsFile)
        {
            //  CreateNewSnapshot(filmsFile);
        }
    }
}