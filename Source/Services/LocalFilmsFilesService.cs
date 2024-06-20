using System.Diagnostics;
using System.IO;
using WatchedFilmsTracker.Source.Views;

namespace WatchedFilmsTracker.Source.Services
{
    internal class LocalFilmsFilesService
    {
        private static readonly string DEFAULT_WATCHED_FILMS_FILENAME = "MyFilms.txt";
        private static string myDataDirectory;
        private static string programDirectory;

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
                CreateMyDataFolderIfNotExist();
                Process.Start("explorer.exe", @myDataDirectory);
            }
        }

        public static void SaveFileInProgramDirectory()
        {
            if (!Directory.Exists(myDataDirectory))
            {
                CreateMyDataFolderIfNotExist();
            }
            ShowMoveFileDialog();
        }

        private static void CopyOriginalFile()
        {
            throw new NotImplementedException(); //todo try to use filemanager
        }

        private static void MoveOriginalFile()
        {
            throw new NotImplementedException(); //todo try to use filemanager
        }

        private static void ShowMoveFileDialog()
        {
            MoveOriginalDialog dialog = new MoveOriginalDialog();
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            dialog.ShowDialog();

            switch (dialog.Result)
            {
                case MoveOriginalDialog.CustomDialogResult.Move:
                    MoveOriginalFile();
                    return;

                case MoveOriginalDialog.CustomDialogResult.Copy:
                    CopyOriginalFile();
                    return;

                case MoveOriginalDialog.CustomDialogResult.Cancel:
                default:
                    return;
            }
        }
    }
}