using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WatchedFilmsTracker.Source.Views
{
    /// <summary>
    /// Interaction logic for RenameColumn.xaml
    /// </summary>
    public partial class RenameColumn : Window
    {
        public CustomDialogResult Result { get; private set; }
        public string newColumnName { get; set; }

        public RenameColumn()
        {
            InitializeComponent();
            Closing += OnDialogClosing;
        }

        public enum CustomDialogResult
        {
            Cancel,
            Confirm
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            Result = CustomDialogResult.Cancel;
            this.Close();
        }

        private void ConfirmButton(object sender, RoutedEventArgs e)
        {
            Result = CustomDialogResult.Confirm;
            newColumnName = TextBlockColumnName.Text;
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