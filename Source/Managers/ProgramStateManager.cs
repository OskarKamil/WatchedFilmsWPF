namespace WatchedFilmsTracker.Source.Managers
{
    internal class ProgramStateManager
    {
        private static bool anyChange;
        private static bool atLeastOneRecord;
        private static bool isFileInLocalMyDataFolder;
        private static bool isFileSavedOnDisk;
        private static MainWindow mainWindow;
        private static bool openedFile;
        private static bool selectedCells;
        private static bool unsavedChange;

        public ProgramStateManager(MainWindow mainWindow)
        {
            ProgramStateManager.mainWindow = mainWindow;
            unsavedChange = false;
            anyChange = false;
        }

        public static bool AtLeastOneRecord
        {
            get => atLeastOneRecord;
            set
            {
                atLeastOneRecord = value;
                if (atLeastOneRecord)
                    ButtonManager.EnableButtons(ButtonManager.AtLeastOneRecordButtons);
                else
                    ButtonManager.DisableButtons(ButtonManager.AtLeastOneRecordButtons);
            }
        }

        public static bool IsAnyChange
        {
            get => anyChange;
            set
            {
                if (!value)
                    ButtonManager.DisableButtons(ButtonManager.AnyChangeButtons);
                else
                {
                    ButtonManager.EnableButtons(ButtonManager.AnyChangeButtons);
                    unsavedChange = true;
                }

                anyChange = value;
            }
        }

        public static bool IsFileInLocalMyDataFolder
        {
            get => isFileInLocalMyDataFolder;
            set
            {
                if (value)
                {
                    ButtonManager.DisableButtons(ButtonManager.FileIsNotInLocalMyDataDirectoryButtons);
                }
                else
                {
                    ButtonManager.EnableButtons(ButtonManager.FileIsNotInLocalMyDataDirectoryButtons);
                }
                isFileInLocalMyDataFolder = value;
            }
        }

        public static bool IsFileSavedOnDisk
        {
            get => isFileSavedOnDisk;
            set
            {
                if (value)
                    ButtonManager.EnableButtons(ButtonManager.FileExistsOnDiskButtons);
                else

                    ButtonManager.DisableButtons(ButtonManager.FileExistsOnDiskButtons);
                isFileSavedOnDisk = value;
            }
        }

        public static bool IsOpenedFile
        {
            get => openedFile;
            set
            {
                openedFile = value;
                mainWindow.UpdateStageTitle();

                if (openedFile)
                    ButtonManager.EnableButtons(ButtonManager.OpenedFileButtons);
                else
                    ButtonManager.DisableButtons(ButtonManager.OpenedFileButtons);
            }
        }

        public static bool IsSelectedCells
        {
            get => selectedCells;
            set
            {
                selectedCells = value;
                if (selectedCells)
                    ButtonManager.EnableButtons(ButtonManager.SelectedCellsButtons);
                else
                    ButtonManager.DisableButtons(ButtonManager.SelectedCellsButtons);
            }
        }

        public static bool IsUnsavedChange
        {
            get
            {
                if (unsavedChange)
                    ButtonManager.EnableButtons(ButtonManager.UnsavedChangeButtons);
                else
                    ButtonManager.DisableButtons(ButtonManager.UnsavedChangeButtons);

                return unsavedChange;
            }
            set
            {
                unsavedChange = value;
                mainWindow.UpdateStageTitle();
            }
        }
    }
}