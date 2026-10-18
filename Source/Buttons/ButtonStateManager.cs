using WatchedFilmsTracker.Source.ManagingFilmsFile;

namespace WatchedFilmsTracker.Source.Buttons
{
    internal static class ButtonStateManager
    {
        public static void UpdateAllButton(CollectionFile workingTextFile)
        {
            UpdateUnsavedChanges(workingTextFile);
            UpdateSelectedCells(workingTextFile);
            UpdateAtLeastOneRecord(workingTextFile);
            UpdateFileExistsOnDiskButtons(workingTextFile);
            UpdateFileIsNotInLocalMyDataDirectoryButtons(workingTextFile);
            UpdateCollectionType(workingTextFile);
        }

        public static void UpdateCollectionType(CollectionFile workingTextFile)
        {
           // throw new NotImplementedException();
        }

        public static void UpdateFileExistsOnDiskButtons(CollectionFile workingTextFile)
        {
            if (workingTextFile.DoesExistOnDisk())
                ButtonManager.EnableButtons(ButtonManager.FileExistsOnDiskButtons);
            else
                ButtonManager.DisableButtons(ButtonManager.FileExistsOnDiskButtons);
        }

        public static void UpdateFileIsNotInLocalMyDataDirectoryButtons(CollectionFile workingTextFile)
        {
            if (workingTextFile.DoesExistInLocalMyDataFolder())
                ButtonManager.EnableButtons(ButtonManager.FileIsNotInLocalMyDataDirectoryButtons);
            else
                ButtonManager.DisableButtons(ButtonManager.FileIsNotInLocalMyDataDirectoryButtons);
        }
        public static void UpdateUnsavedChanges(CollectionFile workingTextFile)
        {
            if (workingTextFile.HasUnsavedChanges)
                ButtonManager.EnableButtons(ButtonManager.UnsavedChangeButtons);
            else
                ButtonManager.DisableButtons(ButtonManager.UnsavedChangeButtons);
        }

        internal static void UpdateAtLeastOneRecord(CollectionFile workingTextFile)
        {
            if (workingTextFile.HasAtLeastOneRecord())
                ButtonManager.EnableButtons(ButtonManager.AtLeastOneRecordButtons);
            else
                ButtonManager.DisableButtons(ButtonManager.AtLeastOneRecordButtons);
        }

        internal static void UpdateSelectedCells(CollectionFile workingTextFile)
        {
            if (workingTextFile.HasSelectedCells())
                ButtonManager.EnableButtons(ButtonManager.SelectedCellsButtons);
            else
                ButtonManager.DisableButtons(ButtonManager.SelectedCellsButtons);
        }
    }
}
