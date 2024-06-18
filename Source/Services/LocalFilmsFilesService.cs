using System.Diagnostics;
using System.IO;

namespace WatchedFilmsTracker.Source.Services
{
    internal class LocalFilmsFilesService
    {
        private static string programDirectory;
        private static string myDataDirectory;
        private static readonly string DEFAULT_WATCHED_FILMS_FILENAME = "MyFilms.txt";

        public static void CreateMyDataFolderIfNotExist()
        {
            programDirectory = Environment.CurrentDirectory;
            myDataDirectory = Path.Combine(programDirectory, "MyData");
            if (!Directory.Exists(myDataDirectory))
            {
                Directory.CreateDirectory(myDataDirectory);
            }
        }

        public static void OpenMyDataDirectory()
        {
            if (Directory.Exists(myDataDirectory))
            {
                Process.Start("explorer.exe", @myDataDirectory);
            }
            else
            {
                Debug.WriteLine($"{myDataDirectory} cannot be found");
            }
        }

        public static void SaveFileInProgramDirectory()
        {
            if (!Directory.Exists(myDataDirectory))
            {
                return;
            }
            
            //todo implement
            // save only if the file is located outside of local directory
            // check if file doesn't already exist with that name
            // after saving here, reopen the file so save changes
            //
        }
    }
}