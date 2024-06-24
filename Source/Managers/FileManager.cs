using System.IO;

namespace WatchedFilmsTracker.Source.Managers
{
    internal class FileManager
    {
        private RecordManager _filmsFile;

        public FileManager(RecordManager filmsFile)
        {
            _filmsFile = filmsFile;
        }

        public FileManager()
        {
        }

        public RecordManager FilmsFile { get => _filmsFile; set => _filmsFile = value; }

        public RecordManager OpenFilepath(string? newFilePath)
        {
            RecordManager recordManager;
            if (string.IsNullOrEmpty(newFilePath) || !(File.Exists(newFilePath)))
            {
                recordManager = new RecordManager();
                recordManager.StartReader();
            }
            else
            {
                recordManager = new RecordManager(newFilePath);
                recordManager.StartReader();
            }
            return recordManager;
        }

        public void SaveFileAtLocation(string filepath)
        {
            _filmsFile.StartWriter(filepath);
            _filmsFile = OpenFilepath(filepath);
        }

        private static void TryToOpenFilepath(string? filepath)
        {
            // just like openfilepath from mainwindow, open filepath but first check if unsaved changes
        }
    }
}