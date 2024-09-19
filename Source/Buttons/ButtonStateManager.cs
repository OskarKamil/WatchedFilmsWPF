using WatchedFilmsTracker.Source.ManagingFilmsFile;

namespace WatchedFilmsTracker.Source.Buttons
{
    internal static class ButtonStateManager
    {
        public static void UpdateAllButton(WorkingTextFile workingTextFile)
        {
            UpdateUnsavedChanges(workingTextFile);
            UpdateSelectedCells(workingTextFile);
            UpdateAtLeastOneRecord(workingTextFile);
            UpdateAnyChange(workingTextFile);
            UpdateFileExistsOnDiskButtons(workingTextFile);
            UpdateFileIsNotInLocalMyDataDirectoryButtons(workingTextFile);
        }

        public static void UpdateAnyChange(WorkingTextFile workingTextFile)
        {
            if (workingTextFile.AnyChange)
                ButtonManager.EnableButtons(ButtonManager.AnyChangeButtons);
            else
                ButtonManager.DisableButtons(ButtonManager.AnyChangeButtons);
        }

        public static void UpdateFileExistsOnDiskButtons(WorkingTextFile workingTextFile)
        {
            if (workingTextFile.DoesExistOnDisk())
                ButtonManager.EnableButtons(ButtonManager.FileExistsOnDiskButtons);
            else
                ButtonManager.DisableButtons(ButtonManager.FileExistsOnDiskButtons);
        }

        public static void UpdateFileIsNotInLocalMyDataDirectoryButtons(WorkingTextFile workingTextFile)
        {
            if (workingTextFile.DoesExistInLocalMyDataFolder())
                ButtonManager.EnableButtons(ButtonManager.FileIsNotInLocalMyDataDirectoryButtons);
            else
                ButtonManager.DisableButtons(ButtonManager.FileIsNotInLocalMyDataDirectoryButtons);
        }
        public static void UpdateUnsavedChanges(WorkingTextFile workingTextFile)
        {
            if (workingTextFile.HasUnsavedChanges())
                ButtonManager.EnableButtons(ButtonManager.UnsavedChangeButtons);
            else
                ButtonManager.DisableButtons(ButtonManager.UnsavedChangeButtons);
        }

        internal static void UpdateAtLeastOneRecord(WorkingTextFile workingTextFile)
        {
            if (workingTextFile.HasAtLeastOneRecord())
                ButtonManager.EnableButtons(ButtonManager.AtLeastOneRecordButtons);
            else
                ButtonManager.DisableButtons(ButtonManager.AtLeastOneRecordButtons);
        }

        internal static void UpdateSelectedCells(WorkingTextFile workingTextFile)
        {
            if (workingTextFile.HasSelectedCells())
                ButtonManager.EnableButtons(ButtonManager.SelectedCellsButtons);
            else
                ButtonManager.DisableButtons(ButtonManager.SelectedCellsButtons);
        }
    }
}