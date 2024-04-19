using System.Windows;

namespace WatchedFilmsTracker.Source.Views
{
    public partial class SaveChangesDialog : Window
    {
        public SaveChangesDialog()
        {
            InitializeComponent();
            Closing += OnDialogClosing;
        }

        public enum CustomDialogResult
        {
            Cancel,
            Save,
            NotSave
        }

        public CustomDialogResult Result { get; private set; }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomDialogResult.Cancel;
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomDialogResult.NotSave;
            this.Close();
        }

        private void OnDialogClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Result == default(CustomDialogResult))
            {
                Result = CustomDialogResult.Cancel;
            }
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomDialogResult.Save;
            this.Close();
        }
    }
}