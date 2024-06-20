using System.Windows;

namespace WatchedFilmsTracker.Source.Views
{
    public partial class MoveOriginalDialog : Window
    {
        public MoveOriginalDialog()
        {
            InitializeComponent();
            Closing += OnDialogClosing;
        }

        public enum CustomDialogResult
        {
            Cancel,
            Move,
            Copy
        }

        public CustomDialogResult Result { get; private set; }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomDialogResult.Cancel;
            this.Close();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomDialogResult.Copy;
            this.Close();
        }

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomDialogResult.Move;
            this.Close();
        }

        private void OnDialogClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Result == default(CustomDialogResult))
            {
                Result = CustomDialogResult.Cancel;
            }
        }
    }
}