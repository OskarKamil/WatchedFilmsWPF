using System.Diagnostics;
using System.IO;
using WatchedFilmsTracker.Source.ManagingFilmsFile;
using WatchedFilmsTracker.Source.Views;

namespace WatchedFilmsTracker.Source.Services
{
    internal class LocalFilmsFilesService
    {
        private static readonly int ARBITRATY_MAX_LOCAL_FILES = 20;
        private static readonly string DEFAULT_FILE_EXTENSION = ".txt";
        private static readonly string DEFAULT_WATCHED_FILMS_FILENAME = "MyFilms";
        private static readonly string WATCHED_FILMS_FILENAME_PATTERN = "MyFilms*.*";
        private static string myDataDirectory;
        private static string programDirectory;
        private FilmsTextFile myFileManager;

        public LocalFilmsFilesService(FilmsTextFile myFileManager)
        {
            this.myFileManager = myFileManager;
        }

        public static void CreateMyDataFolderIfNotExist()
        {
            programDirectory = Environment.CurrentDirectory;
            myDataDirectory = Path.Combine(programDirectory, "MyData");
            if (!Directory.Exists(myDataDirectory))
            {
                Directory.CreateDirectory(myDataDirectory);
            }
        }

        public static void OpenMyDataDirectoryFileExplorer()
        {
            if (Directory.Exists(myDataDirectory))
            {
                Process.Start("explorer.exe", @myDataDirectory);
            }
            else
            {
                CreateMyDataFolderIfNotExist();
                Process.Start("explorer.exe", @myDataDirectory);
            }
        }

        public bool SaveFileInProgramDirectory()
        {
            if (!Directory.Exists(myDataDirectory))
            {
                CreateMyDataFolderIfNotExist();
            }
            return ShowMoveFileDialog();
        }

        private void CopyOriginalFile()
        {
            DirectoryInfo d = new DirectoryInfo(@myDataDirectory);
            List<FileInfo> files = d.GetFiles(WATCHED_FILMS_FILENAME_PATTERN).ToList();
            Debug.WriteLine($"here files.arraylist is {files.Count}");
            if (files.Count == 0)
            {
                myFileManager.SaveFileAtLocation(Path.Combine(myDataDirectory, DEFAULT_WATCHED_FILMS_FILENAME + DEFAULT_FILE_EXTENSION));
                myFileManager.OpenFilepath(Path.Combine(myDataDirectory, DEFAULT_WATCHED_FILMS_FILENAME + DEFAULT_FILE_EXTENSION));
                return;
            }
            //foreach (FileInfo file in files)
            //{
            //    Debug.WriteLine(file.FullName);
            //}
            for (int i = 1; i < ARBITRATY_MAX_LOCAL_FILES; i++)
            {
                string currentFileNameCheck = Path.Combine(myDataDirectory, DEFAULT_WATCHED_FILMS_FILENAME + $" ({i})" + DEFAULT_FILE_EXTENSION);
                if (!File.Exists(currentFileNameCheck))
                {
                    myFileManager.SaveFileAtLocation(currentFileNameCheck);
                    myFileManager.OpenFilepath(currentFileNameCheck);
                    return;
                }
            }
        }

        private void MoveOriginalFile()
        {
            string fileToDelete = myFileManager.FilmsFile.FilePath;
            CopyOriginalFile();
            File.Delete(fileToDelete);
        }

        private bool ShowMoveFileDialog()
        {
            MoveOriginalDialog dialog = new MoveOriginalDialog();
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();

            switch (dialog.Result)
            {
                case MoveOriginalDialog.CustomDialogResult.Move:
                    MoveOriginalFile();
                    return true;

                case MoveOriginalDialog.CustomDialogResult.Copy:
                    CopyOriginalFile();
                    return true;

                case MoveOriginalDialog.CustomDialogResult.Cancel:
                default:
                    return false;
            }
        }
    }
}