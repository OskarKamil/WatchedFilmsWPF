using System.Windows.Controls;

namespace WatchedFilmsTracker.Source.Buttons
{
    public static class ButtonManager
    {
        public static List<Button> AlwaysActiveButtons { get; } = new List<Button>();
        public static List<Button> AnyChangeButtons { get; } = new List<Button>();
        public static List<Button> AtLeastOneRecordButtons { get; } = new List<Button>();
        public static List<Button> FileExistsOnDiskButtons { get; } = new List<Button>();
        public static List<Button> FileIsNotInLocalMyDataDirectoryButtons { get; } = new List<Button>();
        public static List<Button> OpenedFileButtons { get; } = new List<Button>();
        public static List<Button> SelectedCellsButtons { get; } = new List<Button>();
        public static List<Button> UnsavedChangeButtons { get; } = new List<Button>();

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

        public static void SetButtonsState(List<Button> buttons, bool result)
        {
            if (result == true)
                EnableButtons(buttons);
            else if (result == false)
                DisableButtons(buttons);
        }
    }
}