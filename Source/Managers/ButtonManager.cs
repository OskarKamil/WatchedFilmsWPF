using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WatchedFilmsTracker.Source.Managers
{
    public static class ButtonManager
    {
        private static List<Button> alwaysActiveButtons = new List<Button>();
        private static List<Button> openedFileButtons = new List<Button>();
        private static List<Button> selectedCellsButtons = new List<Button>();
        private static List<Button> unsavedChangeButtons = new List<Button>();
        private static List<Button> anyChangeButtons = new List<Button>();

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

        public static List<Button> GetAlwaysActiveButtons()
        {
            return alwaysActiveButtons;
        }

        public static void SetAlwaysActiveButtons(List<Button> alwaysActiveButtons)
        {
            ButtonManager.alwaysActiveButtons = alwaysActiveButtons;
        }

        public static List<Button> GetAnyChangeButtons()
        {
            return anyChangeButtons;
        }

        public static void SetAnyChangeButtons(List<Button> anyChangeButtons)
        {
            ButtonManager.anyChangeButtons = anyChangeButtons;
        }

        public static List<Button> GetOpenedFileButtons()
        {
            return openedFileButtons;
        }

        public static void SetOpenedFileButtons(List<Button> openedFileButtons)
        {
            ButtonManager.openedFileButtons = openedFileButtons;
        }

        public static List<Button> GetSelectedCellsButtons()
        {
            return selectedCellsButtons;
        }

        public static void SetSelectedCellsButtons(List<Button> selectedCellsButtons)
        {
            ButtonManager.selectedCellsButtons = selectedCellsButtons;
        }

        public static List<Button> GetUnsavedChangeButtons()
        {
            return unsavedChangeButtons;
        }

        public static void SetUnsavedChangeButtons(List<Button> unsavedChangeButtons)
        {
            ButtonManager.unsavedChangeButtons = unsavedChangeButtons;
        }

        public static void TestButtons(bool b)
        {
            List<Button> allButtons = new List<Button>();
            allButtons.AddRange(alwaysActiveButtons);
            allButtons.AddRange(openedFileButtons);
            allButtons.AddRange(selectedCellsButtons);
            allButtons.AddRange(unsavedChangeButtons);
            allButtons.AddRange(anyChangeButtons);
            foreach (Button button in allButtons)
            {
                button.Content = "Good";
            }
        }
    }
}