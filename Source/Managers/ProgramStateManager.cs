using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchedFilmsTracker.Source.Managers
{
    internal class ProgramStateManager
    {
        private static MainWindow mainWindow;
        private static bool unsavedChange;
        private static bool anyChange;
        private static bool selectedCells;
        private static bool openedFile;

        public ProgramStateManager(MainWindow mainWindow)
        {
            ProgramStateManager.mainWindow = mainWindow;
            unsavedChange = false;
            anyChange = false;
        }

        public static bool IsAnyChange
        {
            get => anyChange;
            set
            {
                if (!value)
                    ButtonManager.DisableButtons(ButtonManager.GetAnyChangeButtons());
                else
                {
                    ButtonManager.EnableButtons(ButtonManager.GetAnyChangeButtons());
                    unsavedChange = true;
                }

                anyChange = value;
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
                    ButtonManager.EnableButtons(ButtonManager.GetOpenedFileButtons());
                else
                    ButtonManager.DisableButtons(ButtonManager.GetOpenedFileButtons());
            }
        }

        public static bool IsSelectedCells
        {
            get => selectedCells;
            set
            {
                selectedCells = value;
                if (selectedCells)
                    ButtonManager.EnableButtons(ButtonManager.GetSelectedCellsButtons());
                else
                    ButtonManager.DisableButtons(ButtonManager.GetSelectedCellsButtons());
            }
        }

        public static bool IsUnsavedChange
        {
            get
            {
                if (unsavedChange)
                    ButtonManager.EnableButtons(ButtonManager.GetUnsavedChangeButtons());
                else
                    ButtonManager.DisableButtons(ButtonManager.GetUnsavedChangeButtons());

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
