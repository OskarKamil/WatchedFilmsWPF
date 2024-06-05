using System.Windows.Controls;

namespace WatchedFilmsTracker.Source.Managers
{
    public static class ButtonManager
    {
        private static List<Button> alwaysActiveButtons = new List<Button>();
        private static List<Button> anyChangeButtons = new List<Button>();
        private static List<Button> atLeastOneRecordButtons = new List<Button>();
        private static List<Button> openedFileButtons = new List<Button>();
        private static List<Button> selectedCellsButtons = new List<Button>();
        private static List<Button> unsavedChangeButtons = new List<Button>();

        public static List<Button> AlwaysActiveButtons { get => alwaysActiveButtons; set => alwaysActiveButtons = value; }
        public static List<Button> AnyChangeButtons { get => anyChangeButtons; set => anyChangeButtons = value; }
        public static List<Button> AtLeastOneRecordButtons { get => atLeastOneRecordButtons; set => atLeastOneRecordButtons = value; }
        public static List<Button> OpenedFileButtons { get => openedFileButtons; set => openedFileButtons = value; }
        public static List<Button> SelectedCellsButtons { get => selectedCellsButtons; set => selectedCellsButtons = value; }
        public static List<Button> UnsavedChangeButtons { get => unsavedChangeButtons; set => unsavedChangeButtons = value; }

        public static void DisableButton(Button button)
        {
            button.IsEnabled = false;
        }

        public static void DisableButtons(List<Button> buttons)
        {
            foreach (Button button in buttons)
            {
                button.IsEnabled = false;
                StackPanel stackPanel = button.Content as StackPanel;
                if (stackPanel != null)
                {
                    foreach (var child in stackPanel.Children)
                    {
                        if (child is Image image)
                        {
                            image.Opacity = 0.50;
                        }
                    }
                }
            }
        }

        public static void EnableButton(Button button)
        {
            button.IsEnabled = true;
        }

        public static void EnableButtons(List<Button> buttons)
        {
            foreach (Button button in buttons)
            {
                button.IsEnabled = true;
                StackPanel stackPanel = button.Content as StackPanel;
                if (stackPanel != null)
                {
                    foreach (var child in stackPanel.Children)
                    {
                        if (child is Image image)
                        {
                            image.Opacity = 1.0;
                        }
                    }
                }
            }
        }

        public static void TestButtons(bool b)
        {
            List<Button> allButtons = new List<Button>();
            allButtons.AddRange(AlwaysActiveButtons);
            allButtons.AddRange(openedFileButtons);
            allButtons.AddRange(SelectedCellsButtons);
            allButtons.AddRange(UnsavedChangeButtons);
            allButtons.AddRange(AnyChangeButtons);
            foreach (Button button in allButtons)
            {
                button.Content = "Good";
            }
        }
    }
}